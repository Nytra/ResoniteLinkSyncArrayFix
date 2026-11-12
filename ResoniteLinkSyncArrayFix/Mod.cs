using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using ResoniteModLoader;
using System.Reflection;
using FrooxEngine.ProtoFlux;

#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace ResoniteLinkSyncArrayFix;

public class ResoniteLinkSyncArrayFix : ResoniteMod
{
	public override string Name => "ResoniteLink SyncArray Fix";
	public override string Author => "Nytra";
	public override string Version => "1.0.0";
	public override string Link => "https://github.com/Nytra/ResoniteResoniteLinkSyncArrayFix";

	private static Harmony harmony = new Harmony("owo.Nytra.ResoniteLinkSyncArrayFix");

	[AutoRegisterConfigKey] static ModConfigurationKey<bool> Key_Enabled = new("enabled", "should the mod be enabled", () => true);

	static ModConfiguration? config;

	public override void OnEngineInit()
	{
		config = GetConfiguration();
#if DEBUG
		HotReloader.RegisterForHotReload(this);
#endif
		Engine.Current.RunPostInit(InitializeMod);
	}

	static void InitializeMod()
	{
		harmony.PatchAll();
	}

#if DEBUG
	static void BeforeHotReload()
	{
		harmony.UnpatchAll(harmony.Id);
	}

	static void OnHotReload(ResoniteMod modInstance)
	{
		config = modInstance.GetConfiguration();
		InitializeMod();
	}
#endif

	[HarmonyPatch]
	class ResoniteLinkSyncArrayFixPatch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			foreach (var type in Coder.BaseEnginePrimitives.Where(t => t != typeof(dummy) && t != typeof(dummy<>)))
			{
				yield return AccessTools.Method(typeof(SyncArray<>).MakeGenericType(type), nameof(SyncArray<>.SetFrom), [typeof(IEnumerable<>).MakeGenericType(type)]);
			}
		}
		static MethodInfo _method = AccessTools.Method(typeof(ResoniteLinkSyncArrayFixPatch), nameof(SetFrom));
		static bool Prefix(object __instance, object values)
		{
			if (!config!.GetValue(Key_Enabled)) return true;

			var type = __instance.GetType().GetGenericArguments()[0];
			try
			{
				_method.MakeGenericMethod(type).Invoke(null, [__instance, values]);
			}
			catch
			{
				Error($"Failed to invoke generic method for type: {type}");
				return true;
			}

			return false;
		}
		static void SetFrom<T>(SyncArray<T> array, IEnumerable<T> values) where T : IEquatable<T>
		{
			int index = 0;
			foreach (T v in values)
			{
				if (index >= array.Count)
				{
					array.Append(v);
				}
				else
				{
					array[index] = v;
				}
				index++;
			}
			array.SetSize(index);
		}
	}
}