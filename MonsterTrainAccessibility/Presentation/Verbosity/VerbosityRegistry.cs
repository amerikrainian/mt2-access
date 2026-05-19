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
        private static readonly Dictionary<Type, ProfileDescriptor> ProfilesBySource =
            new Dictionary<Type, ProfileDescriptor>();

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

                ProfileDescriptor descriptor = BuildDescriptor(attribute, config, root);
                ProfilesBySource[sourceType] = descriptor;
            }

            global::MonsterTrainAccessibility.ModSettings.ModSettings.Register(root);
        }

        public static VerbosityProfile ForSource<TSource>()
        {
            return ForSource(typeof(TSource));
        }

        public static VerbosityProfile ForSource(Type sourceType)
        {
            if (sourceType != null &&
                ProfilesBySource.TryGetValue(sourceType, out ProfileDescriptor descriptor))
            {
                return descriptor.Snapshot();
            }

            return VerbosityProfile.Default;
        }

        private static ProfileDescriptor BuildDescriptor(
            VerbosityProfileAttribute attribute,
            ConfigFile config,
            CategorySetting root)
        {
            List<PresentationSlot> order = OrderedSlots(attribute.DefaultOrder);
            string section = "Verbosity." + attribute.Key;
            CategorySetting profileCategory = new CategorySetting(
                attribute.Key,
                Message.Localized("ui", "VERBOSITY_SETTINGS." + attribute.Key.ToUpperInvariant() + ".LABEL"))
            {
                ReorderableChildren = true
            };

            Dictionary<PresentationSlot, BoolSetting> enabled = new Dictionary<PresentationSlot, BoolSetting>();
            Dictionary<PresentationSlot, BoolSetting> verbose = new Dictionary<PresentationSlot, BoolSetting>();
            Dictionary<PresentationSlot, CategorySetting> slotCategories =
                new Dictionary<PresentationSlot, CategorySetting>();
            HashSet<PresentationSlot> defaultEnabled = new HashSet<PresentationSlot>(attribute.DefaultOrder);
            defaultEnabled.Add(PresentationSlot.Title);
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
                    BoolSetting enabledSetting = new BoolSetting(
                        config,
                        section,
                        slot + ".enabled",
                        Message.Localized("ui", "VERBOSITY_SETTINGS.ENABLED"),
                        defaultEnabled.Contains(slot),
                        "Whether this presentation part is announced.");
                    slotCategory.Add(enabledSetting);
                    enabled[slot] = enabledSetting;
                }

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

            ProfileDescriptor descriptor = new ProfileDescriptor(order, orderEntry, enabled, verbose, slotCategories);
            descriptor.ApplyOrderToSortPriorities();
            profileCategory.OnChildSwap = descriptor.SwapSettings;
            profileCategory.Add(new ActionSetting(
                "reset_to_defaults",
                Message.Localized("ui", "VERBOSITY_SETTINGS.RESET_PROFILE"),
                descriptor.ResetToDefaults,
                Message.Localized("ui", "VERBOSITY_SETTINGS.RESET_PROFILE_DONE"),
                rebuildScreenOnActivate: true));
            root.Add(profileCategory);
            return descriptor;
        }

        private static List<PresentationSlot> OrderedSlots(PresentationSlot[] defaultOrder)
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

            foreach (PresentationSlot slot in Enum.GetValues(typeof(PresentationSlot)))
            {
                if (!order.Contains(slot))
                {
                    order.Add(slot);
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
                    if (Enum.TryParse(part, ignoreCase: true, out PresentationSlot slot) && !order.Contains(slot))
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

        private static Type ResolveSourceType(Type markerType)
        {
            PropertyInfo property = markerType.GetProperty("SourceType", BindingFlags.Public | BindingFlags.Static);
            return property != null && property.PropertyType == typeof(Type)
                ? property.GetValue(null, null) as Type
                : null;
        }

        private sealed class ProfileDescriptor
        {
            private readonly IReadOnlyList<PresentationSlot> _order;
            private readonly ConfigEntry<string> _orderEntry;
            private readonly IReadOnlyDictionary<PresentationSlot, BoolSetting> _enabled;
            private readonly IReadOnlyDictionary<PresentationSlot, BoolSetting> _verbose;
            private readonly IReadOnlyDictionary<PresentationSlot, CategorySetting> _slotCategories;

            public ProfileDescriptor(
                IReadOnlyList<PresentationSlot> order,
                ConfigEntry<string> orderEntry,
                IReadOnlyDictionary<PresentationSlot, BoolSetting> enabled,
                IReadOnlyDictionary<PresentationSlot, BoolSetting> verbose,
                IReadOnlyDictionary<PresentationSlot, CategorySetting> slotCategories)
            {
                _order = order;
                _orderEntry = orderEntry;
                _enabled = enabled;
                _verbose = verbose;
                _slotCategories = slotCategories;
            }

            public VerbosityProfile Snapshot()
            {
                List<PresentationSlot> orderedSlots = OrderedSlots(_orderEntry.Value, _order);
                List<VerbositySlotEntry> entries = new List<VerbositySlotEntry>(orderedSlots.Count);
                for (int i = 0; i < orderedSlots.Count; i++)
                {
                    PresentationSlot slot = orderedSlots[i];
                    bool enabled = slot == PresentationSlot.Title ||
                        (_enabled.TryGetValue(slot, out BoolSetting enabledSetting) && enabledSetting.Value);
                    bool verbose = !_verbose.TryGetValue(slot, out BoolSetting verboseSetting) ||
                        verboseSetting.Value;
                    entries.Add(new VerbositySlotEntry(slot, enabled, verbose));
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
                ResetMap(_enabled);
                ResetMap(_verbose);
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
