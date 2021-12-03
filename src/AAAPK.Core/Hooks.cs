using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;

using HarmonyLib;

using JetPack;

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
				ChaFileAccessory.PartsInfo _part = JetPack.Accessory.GetPartsInfo(__instance, slotNo);
				if (_part == null || _part.type == 120) return false;
				GameObject _ca_slot = JetPack.Accessory.GetObjAccessory(__instance, slotNo);
				if (_ca_slot == null)
				{
					_logger.LogError($"[ChaControl_ChangeShakeAccessory_Prefix] ca_slot{slotNo:00} is null");
					return false;
				}
				ComponentLookupTable _lookup = _ca_slot.GetComponent<ComponentLookupTable>();
				if (_lookup == null)
				{
					_logger.LogError($"[ChaControl_ChangeShakeAccessory_Prefix] ComponentLookupTable at {_ca_slot.name} is null");
					return false;
				}
				List<object> _cmps = _lookup.Components(typeof(DynamicBone));
				if (_cmps?.Count == 0) return false;

				bool _noShake = _part.noShake;

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

				ComponentLookupTable _lookup = __instance.objHair[parts].GetComponent<ComponentLookupTable>();
				if (_lookup == null)
				{
					_logger.LogError($"[ChaControl_ChangeShakeHair_Prefix] ComponentLookupTable at {__instance.objHair[parts].name} is null");
					return false;
				}

				List<object> _cmps = _lookup.Components(typeof(DynamicBone));
				if (_cmps?.Count == 0) return false;

				bool _noShake = __instance.fileHair.parts[parts].noShake;
				foreach (DynamicBone _cmp in _cmps)
				{
					if (_cmp == null || _cmp.m_Root == null) continue;

					if (_cmp.m_Root != null)
						_cmp.enabled = !_noShake;
				}

				return false;
			}

			internal static void MaterialAPI_GetRendererList_Postfix(ref IEnumerable<Renderer> __result, GameObject gameObject)
			{
				if (gameObject == null) return;
				ComponentLookupTable _lookup = gameObject.GetComponent<ComponentLookupTable>();
				if (_lookup == null) return;

				List<Renderer> _filter = new List<Renderer>();
				_filter.AddRange(_lookup.Components<Renderer>());

				if (gameObject.name == "ct_clothesTop" && gameObject.transform.Find("ct_top_parts_A") != null)
				{
					foreach (Transform _child in gameObject.transform)
						_filter.AddRange(_child.GetComponent<ComponentLookupTable>().Components<Renderer>());
				}

				__result = _filter.AsEnumerable();
			}

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
					ComponentLookupTable _lookup = _cmp.GetComponent<ComponentLookupTable>();
					if (_lookup == null) continue;

					if (_lookup.Components<DynamicBone>().Contains(_result[0]))
						return _lookup.Components<DynamicBone>().Where(x => x.m_Root != null && !x.m_Root.name.StartsWith("cf_j_sk_") && !x.m_Root.name.StartsWith("cf_d_sk_")).ToArray();
				}

				return _result;
			}
		}
	}
}
