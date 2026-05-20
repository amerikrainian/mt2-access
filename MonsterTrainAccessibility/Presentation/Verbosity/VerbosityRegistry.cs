using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.ModSettings;

namespace MonsterTrainAccessibility.Presentation.Verbosity
{
    internal static class VerbosityRegistry
    {
        private static readonly Dictionary<Type, List<ProfileDescriptor>> ProfilesBySource =
            new Dictionary<Type, List<ProfileDescriptor>>();

        private static bool _initialized;

        public static void Initialize(ConfigFile config)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            CategorySetting root = new CategorySetting(
                "verbosity",
                Message.Localized("ui", "VERBOSITY_SETTINGS.CATEGORY"));

            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                Type markerType = types[i];
                VerbosityProfileAttribute attribute =
                    markerType.GetCustomAttribute(typeof(VerbosityProfileAttribute)) as VerbosityProfileAttribute;
                if (attribute == null)
                {
                    continue;
                }

                Type sourceType = ResolveSourceType(markerType);
                if (sourceType == null)
                {
                    continue;
                }

                ProfileDescriptor descriptor = BuildDescriptor(markerType, attribute, config, root);
                if (!ProfilesBySource.TryGetValue(sourceType, out List<ProfileDescriptor> descriptors))
                {
                    descriptors = new List<ProfileDescriptor>();
                    ProfilesBySource[sourceType] = descriptors;
                }

                descriptors.Add(descriptor);
            }

            foreach (List<ProfileDescriptor> descriptors in ProfilesBySource.Values)
            {
                descriptors.Sort((left, right) => right.MatchPriority.CompareTo(left.MatchPriority));
            }

            global::MonsterTrainAccessibility.ModSettings.ModSettings.Register(root);
        }

        public static VerbosityProfile ForSource<TSource>()
        {
            return ForSource(typeof(TSource));
        }

        public static VerbosityProfile ForSource<TSource>(TSource source)
        {
            if (source != null &&
                ProfilesBySource.TryGetValue(typeof(TSource), out List<ProfileDescriptor> descriptors))
            {
                ProfileDescriptor fallback = null;
                for (int i = 0; i < descriptors.Count; i++)
                {
                    ProfileDescriptor descriptor = descriptors[i];
                    if (!descriptor.HasMatcher)
                    {
                        fallback = fallback ?? descriptor;
                        continue;
                    }

                    if (descriptor.Matches(source))
                    {
                        return descriptor.Snapshot();
                    }
                }

                if (fallback != null)
                {
                    return fallback.Snapshot();
                }
            }

            return ForSource(typeof(TSource));
        }

        public static VerbosityProfile ForSource(Type sourceType)
        {
            if (sourceType != null &&
                ProfilesBySource.TryGetValue(sourceType, out List<ProfileDescriptor> descriptors))
            {
                for (int i = 0; i < descriptors.Count; i++)
                {
                    if (!descriptors[i].HasMatcher)
                    {
                        return descriptors[i].Snapshot();
                    }
                }

                if (descriptors.Count > 0)
                {
                    return descriptors[0].Snapshot();
                }
            }

            return VerbosityProfile.Default;
        }

        private static ProfileDescriptor BuildDescriptor(
            Type markerType,
            VerbosityProfileAttribute attribute,
            ConfigFile config,
            CategorySetting root)
        {
            List<PresentationSlot> order = OrderedSlots(attribute.DefaultOrder, attribute.SupportedSlots);
            string section = "Verbosity." + attribute.Key;
            CategorySetting profileCategory = new CategorySetting(
                attribute.Key,
                Message.Localized("ui", "VERBOSITY_SETTINGS." + attribute.Key.ToUpperInvariant() + ".LABEL"))
            {
                ReorderableChildren = true
            };

            Dictionary<PresentationSlot, BoolSetting> showInDetails = new Dictionary<PresentationSlot, BoolSetting>();
            Dictionary<PresentationSlot, BoolSetting> verbose = new Dictionary<PresentationSlot, BoolSetting>();
            Dictionary<PresentationSlot, BoolSetting> inSummary = new Dictionary<PresentationSlot, BoolSetting>();
            Dictionary<PresentationSlot, CategorySetting> slotCategories =
                new Dictionary<PresentationSlot, CategorySetting>();
            HashSet<PresentationSlot> defaultShownInDetails = new HashSet<PresentationSlot>(attribute.DefaultOrder);
            defaultShownInDetails.Add(PresentationSlot.Title);
            ConfigEntry<string> orderEntry = config.Bind(
                section,
                "order",
                ToOrderCsv(order),
                new ConfigDescription(
                    "Comma-separated presentation slot order. Unknown names are ignored; missing slots are appended."));

            for (int i = 0; i < order.Count; i++)
            {
                PresentationSlot slot = order[i];
                CategorySetting slotCategory = new CategorySetting(
                    slot.ToString().ToLowerInvariant(),
                    Message.Localized("ui", "VERBOSITY_SETTINGS.SLOT." + slot.ToString().ToUpperInvariant()))
                {
                    CanConfigure = slot != PresentationSlot.Title
                };
                slotCategories[slot] = slotCategory;

                if (slot != PresentationSlot.Title)
                {
                    BoolSetting showInDetailsSetting = new BoolSetting(
                        config,
                        section,
                        slot + ".show_in_details",
                        Message.Localized("ui", "VERBOSITY_SETTINGS.SHOW_IN_DETAILS"),
                        defaultShownInDetails.Contains(slot),
                        "Whether this presentation part is shown in details.");
                    slotCategory.Add(showInDetailsSetting);
                    showInDetails[slot] = showInDetailsSetting;
                }

                BoolSetting inSummarySetting = new BoolSetting(
                    config,
                    section,
                    slot + ".in_summary",
                    Message.Localized("ui", "VERBOSITY_SETTINGS.IN_SUMMARY"),
                    DefaultInSummary(slot),
                    "Whether this presentation part is included in focus speech.");
                slotCategory.Add(inSummarySetting);
                inSummary[slot] = inSummarySetting;

                if (SlotSupportsVerbose(slot))
                {
                    BoolSetting verboseSetting = new BoolSetting(
                        config,
                        section,
                        slot + ".verbose",
                        Message.Localized("ui", "VERBOSITY_SETTINGS.VERBOSE"),
                        true,
                        "Whether this presentation part uses full labels.");
                    slotCategory.Add(verboseSetting);
                    verbose[slot] = verboseSetting;
                }

                profileCategory.Add(slotCategory);
            }

            ProfileDescriptor descriptor = new ProfileDescriptor(
                order,
                orderEntry,
                showInDetails,
                verbose,
                inSummary,
                slotCategories,
                ResolveMatcher(markerType),
                attribute.MatchPriority);
            descriptor.ApplyOrderToSortPriorities();
            profileCategory.OnChildSwap = descriptor.SwapSettings;
            profileCategory.Add(new ActionSetting(
                "reset_to_defaults",
                Message.Localized("ui", "VERBOSITY_SETTINGS.RESET_PROFILE"),
                descriptor.ResetToDefaults,
                Message.Localized("ui", "VERBOSITY_SETTINGS.RESET_PROFILE_DONE"),
                rebuildScreenOnActivate: true));
            ParentCategory(root, attribute).Add(profileCategory);
            return descriptor;
        }

        private static CategorySetting ParentCategory(CategorySetting root, VerbosityProfileAttribute attribute)
        {
            if (string.IsNullOrWhiteSpace(attribute.GroupKey))
            {
                return root;
            }

            IReadOnlyList<Setting> children = root.Children;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is CategorySetting category &&
                    string.Equals(category.Key, attribute.GroupKey, StringComparison.Ordinal))
                {
                    return category;
                }
            }

            CategorySetting group = new CategorySetting(
                attribute.GroupKey,
                Message.Localized("ui", "VERBOSITY_SETTINGS." + attribute.GroupKey.ToUpperInvariant() + ".LABEL"));
            root.Add(group);
            return group;
        }

        private static List<PresentationSlot> OrderedSlots(PresentationSlot[] defaultOrder, PresentationSlot[] supportedSlots)
        {
            List<PresentationSlot> order = new List<PresentationSlot>();
            if (defaultOrder != null)
            {
                for (int i = 0; i < defaultOrder.Length; i++)
                {
                    if (!order.Contains(defaultOrder[i]))
                    {
                        order.Add(defaultOrder[i]);
                    }
                }
            }

            if (supportedSlots != null)
            {
                for (int i = 0; i < supportedSlots.Length; i++)
                {
                    if (!order.Contains(supportedSlots[i]))
                    {
                        order.Add(supportedSlots[i]);
                    }
                }
            }

            return order;
        }

        private static string ToOrderCsv(IReadOnlyList<PresentationSlot> order)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < order.Count; i++)
            {
                names.Add(order[i].ToString().ToLowerInvariant());
            }

            return string.Join(",", names.ToArray());
        }

        private static List<PresentationSlot> OrderedSlots(string orderCsv, IReadOnlyList<PresentationSlot> fallbackOrder)
        {
            List<PresentationSlot> order = new List<PresentationSlot>();
            if (!string.IsNullOrWhiteSpace(orderCsv))
            {
                string[] parts = orderCsv.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i].Trim();
                    if (Enum.TryParse(part, ignoreCase: true, out PresentationSlot slot) &&
                        fallbackOrder.Contains(slot) &&
                        !order.Contains(slot))
                    {
                        order.Add(slot);
                    }
                }
            }

            for (int i = 0; i < fallbackOrder.Count; i++)
            {
                if (!order.Contains(fallbackOrder[i]))
                {
                    order.Add(fallbackOrder[i]);
                }
            }

            return order;
        }

        private static bool SlotSupportsVerbose(PresentationSlot slot)
        {
            switch (slot)
            {
                case PresentationSlot.Cost:
                case PresentationSlot.Stats:
                case PresentationSlot.Subtitle:
                    return true;
                default:
                    return false;
            }
        }

        private static bool DefaultInSummary(PresentationSlot slot)
        {
            switch (slot)
            {
                case PresentationSlot.Title:
                case PresentationSlot.Subtitle:
                case PresentationSlot.Cost:
                case PresentationSlot.Stats:
                case PresentationSlot.Description:
                case PresentationSlot.Ability:
                case PresentationSlot.Trigger:
                case PresentationSlot.Status:
                    return true;
                default:
                    return false;
            }
        }

        private static Type ResolveSourceType(Type markerType)
        {
            PropertyInfo property = markerType.GetProperty("SourceType", BindingFlags.Public | BindingFlags.Static);
            return property != null && property.PropertyType == typeof(Type)
                ? property.GetValue(null, null) as Type
                : null;
        }

        private static Func<object, bool> ResolveMatcher(Type markerType)
        {
            MethodInfo method = markerType.GetMethod("Matches", BindingFlags.Public | BindingFlags.Static);
            if (method == null || method.ReturnType != typeof(bool))
            {
                return null;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 1)
            {
                return null;
            }

            return source =>
                source != null &&
                parameters[0].ParameterType.IsInstanceOfType(source) &&
                (bool)method.Invoke(null, new[] { source });
        }

        private sealed class ProfileDescriptor
        {
            private readonly IReadOnlyList<PresentationSlot> _order;
            private readonly ConfigEntry<string> _orderEntry;
            private readonly IReadOnlyDictionary<PresentationSlot, BoolSetting> _showInDetails;
            private readonly IReadOnlyDictionary<PresentationSlot, BoolSetting> _verbose;
            private readonly IReadOnlyDictionary<PresentationSlot, BoolSetting> _inSummary;
            private readonly IReadOnlyDictionary<PresentationSlot, CategorySetting> _slotCategories;
            private readonly Func<object, bool> _matcher;

            public ProfileDescriptor(
                IReadOnlyList<PresentationSlot> order,
                ConfigEntry<string> orderEntry,
                IReadOnlyDictionary<PresentationSlot, BoolSetting> showInDetails,
                IReadOnlyDictionary<PresentationSlot, BoolSetting> verbose,
                IReadOnlyDictionary<PresentationSlot, BoolSetting> inSummary,
                IReadOnlyDictionary<PresentationSlot, CategorySetting> slotCategories,
                Func<object, bool> matcher,
                int matchPriority)
            {
                _order = order;
                _orderEntry = orderEntry;
                _showInDetails = showInDetails;
                _verbose = verbose;
                _inSummary = inSummary;
                _slotCategories = slotCategories;
                _matcher = matcher;
                MatchPriority = matchPriority;
            }

            public int MatchPriority { get; }
            public bool HasMatcher => _matcher != null;

            public bool Matches(object source)
            {
                return _matcher != null && _matcher(source);
            }

            public VerbosityProfile Snapshot()
            {
                List<PresentationSlot> orderedSlots = OrderedSlots(_orderEntry.Value, _order);
                List<VerbositySlotEntry> entries = new List<VerbositySlotEntry>(orderedSlots.Count);
                for (int i = 0; i < orderedSlots.Count; i++)
                {
                    PresentationSlot slot = orderedSlots[i];
                    bool showInDetails = slot == PresentationSlot.Title ||
                        (_showInDetails.TryGetValue(slot, out BoolSetting showInDetailsSetting) && showInDetailsSetting.Value);
                    bool verbose = !_verbose.TryGetValue(slot, out BoolSetting verboseSetting) ||
                        verboseSetting.Value;
                    bool inSummary = _inSummary.TryGetValue(slot, out BoolSetting inSummarySetting) &&
                        inSummarySetting.Value;
                    entries.Add(new VerbositySlotEntry(slot, showInDetails, verbose, inSummary));
                }

                return new VerbosityProfile(entries);
            }

            public void SwapSettings(Setting first, Setting second)
            {
                if (!TrySlot(first, out PresentationSlot firstSlot) ||
                    !TrySlot(second, out PresentationSlot secondSlot))
                {
                    return;
                }

                List<PresentationSlot> order = OrderedSlots(_orderEntry.Value, _order);
                int firstIndex = order.IndexOf(firstSlot);
                int secondIndex = order.IndexOf(secondSlot);
                if (firstIndex < 0 || secondIndex < 0 || firstIndex == secondIndex)
                {
                    return;
                }

                PresentationSlot temporary = order[firstIndex];
                order[firstIndex] = order[secondIndex];
                order[secondIndex] = temporary;
                _orderEntry.Value = ToOrderCsv(order);
                ApplyOrderToSortPriorities(order);
            }

            public bool ResetToDefaults()
            {
                _orderEntry.Value = ToOrderCsv(_order);
                ResetMap(_showInDetails);
                ResetMap(_verbose);
                ResetMap(_inSummary);
                ApplyOrderToSortPriorities(_order);
                return true;
            }

            public void ApplyOrderToSortPriorities()
            {
                ApplyOrderToSortPriorities(OrderedSlots(_orderEntry.Value, _order));
            }

            private void ApplyOrderToSortPriorities(IReadOnlyList<PresentationSlot> order)
            {
                for (int i = 0; i < order.Count; i++)
                {
                    if (_slotCategories.TryGetValue(order[i], out CategorySetting category))
                    {
                        category.SortPriority = i;
                    }
                }
            }

            private static bool TrySlot(Setting setting, out PresentationSlot slot)
            {
                if (setting != null && Enum.TryParse(setting.Key, ignoreCase: true, out slot))
                {
                    return true;
                }

                slot = default;
                return false;
            }

            private static void ResetMap(IReadOnlyDictionary<PresentationSlot, BoolSetting> settings)
            {
                foreach (KeyValuePair<PresentationSlot, BoolSetting> pair in settings)
                {
                    pair.Value?.ResetToDefault();
                }
            }
        }
    }
}
