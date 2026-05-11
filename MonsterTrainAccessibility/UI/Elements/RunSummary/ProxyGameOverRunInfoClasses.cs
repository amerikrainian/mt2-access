using System.Reflection;
using HarmonyLib;
namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyGameOverRunInfoClasses : ProxyGameOverChosenClasses
    {
        private static readonly FieldInfo ClassesUIField = AccessTools.Field(typeof(global::RunInfo), "classesUI")!;

        public ProxyGameOverRunInfoClasses(global::RunInfo runInfo)
            : base(Get<global::ChosenClassesUI>(runInfo, ClassesUIField))
        {
        }

        private static T Get<T>(object owner, FieldInfo field) where T : class
        {
            return owner != null ? field.GetValue(owner) as T : null;
        }
    }
}
