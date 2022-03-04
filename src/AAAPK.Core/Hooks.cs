using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;

using BepInEx.Logging;
using HarmonyLib;

using KKAPI.Maker;
using MoreAccessoriesKOI;

namespace AAAPK
{
	public partial class AAAPK
	{
		internal static partial class Hooks
		{
			internal static bool ReturnFalse() => false;

			internal static bool ChaControl_ChangeShakeAccessory_Prefix(ChaControl __instance, int slotNo)
			{
				if (__instance == null || slotNo < 0) return true;

				GameObject _ca_slot = AccessoriesApi.GetAccessoryObject(__instance, slotNo);
				if (_ca_slot == null)
				{
					DebugMsg(LogLevel.Error, $"[ChaControl_ChangeShakeAccessory_Prefix] ca_slot{slotNo:00} is null");
					return false;
				}

				Component[] _cmps = _ca_slot.GetComponents(typeof(DynamicBone));
				if (_cmps == null)
				{
					DebugMsg(LogLevel.Error, $"[ChaControl_ChangeShakeAccessory_Prefix] _cmps at {_ca_slot.name} is null");
					return false;
				}
				if (_cmps.Length == 0) return false;

				
				bool _noShake = AccessoriesApi.GetPartsInfo(slotNo).noShake;

				foreach (DynamicBone _cmp in _cmps)
				{
					if (_cmp == null || _cmp.m_Root == null) continue;

					if (_cmp.m_Root != null)
						_cmp.enabled = !_noShake;
				}

				return false;
			}

			internal static bool ChaControl_ChangeShakeHair_Prefix(ChaControl __instance, int parts)
			{
				if (__instance?.fileHair?.parts?.ElementAtOrDefault(parts) == null) return false;
				if (__instance?.objHair.ElementAtOrDefault(parts) == null) return false;

				Component[] _cmps = __instance.objHair[parts].GetComponents(typeof(DynamicBone));
				if (_cmps == null)
				{
					DebugMsg(LogLevel.Error, $"[ChaControl_ChangeShakeAccessory_Prefix] _cmps at {__instance.objHair[parts].name} is null");
					return false;
				}
				if (_cmps.Length == 0) return false;

				bool _noShake = __instance.fileHair.parts[parts].noShake;
				foreach (DynamicBone _cmp in _cmps)
				{
					if (_cmp == null || _cmp.m_Root == null) continue;

					if (_cmp.m_Root != null)
						_cmp.enabled = !_noShake;
				}

				return false;
			}

			//internal static void MaterialAPI_GetRendererList_Postfix(ref IEnumerable<Renderer> __result, GameObject gameObject)
			//{
			//	if (gameObject == null) return;
			//	ComponentLookupTable _lookup = gameObject.GetComponent<ComponentLookupTable>();
			//	if (_lookup == null) return;

			//	List<Renderer> _filter = new List<Renderer>();
			//	_filter.AddRange(_lookup.Components<Renderer>());

			//	if (gameObject.name == "ct_clothesTop" && gameObject.transform.Find("ct_top_parts_A") != null)
			//	{
			//		foreach (Transform _child in gameObject.transform)
			//			_filter.AddRange(_child.GetComponent<Renderer>().Components<Renderer>());
			//	}

			//	__result = _filter.AsEnumerable();
			//}

			internal static IEnumerable<CodeInstruction> CharaController_ApplyData_Transpiler(IEnumerable<CodeInstruction> _instructions)
			{
				MethodInfo _getComponentsInChildrenMethod = AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponentsInChildren), generics: new Type[] { typeof(DynamicBone) });
				if (_getComponentsInChildrenMethod == null)
				{
					_logger.LogError("Failed to get methodinfo for UnityEngine.GameObject.GetComponentsInChildren<DynamicBone>, CharaController_ApplyData_Transpiler will not patch");
					return _instructions;
				}

				CodeMatcher _codeMatcher = new CodeMatcher(_instructions)
					.MatchForward(useEnd: false,
						new CodeMatch(OpCodes.Callvirt, operand: _getComponentsInChildrenMethod),
						new CodeMatch(OpCodes.Stloc_S))
					.Advance(1)
					.InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Hooks), nameof(Hooks.CharaController_ApplyData_Method))));

				_codeMatcher.ReportFailure(MethodBase.GetCurrentMethod(), error => _logger.LogError(error));
				return _codeMatcher.Instructions();
			}

			internal static DynamicBone[] CharaController_ApplyData_Method(DynamicBone[] _getFromStack)
			{
				DynamicBone[] _result = _getFromStack == null ? new DynamicBone[0] : _getFromStack.Where(x => x != null && x.m_Root != null).ToArray();
				if (_result?.Length == 0) return _result;

				ListInfoComponent[] _cmps = _result[0].GetComponentsInParent<ListInfoComponent>(true);
				if (_cmps?.Length == 0) return _result;

				foreach (ListInfoComponent _cmp in _cmps)
				{
					_logger.LogDebug("[CharaController_ApplyData_Method] casting components as DynamicBones array");
					DynamicBone[] _lookup = (DynamicBone[])_cmp.GetComponents(typeof(DynamicBone));
					if (_lookup == null) continue;

					if (_lookup.Contains(_result[0]))
						return _lookup.Where(x => x.m_Root != null && !x.m_Root.name.StartsWith("cf_j_sk_") && !x.m_Root.name.StartsWith("cf_d_sk_")).ToArray();
				}

				return _result;
			}
		}
	}
}
