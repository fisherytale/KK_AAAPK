using KKAPI.Chara;
using KK_Plugins.MaterialEditor;

namespace AAAPK
{
	public partial class AAAPK
	{
		internal static void OnChangeCoordinateType(ChaControl _chaCtrl)
		{
			AAAPKController _pluginCtrl = GetController(_chaCtrl);
			if (_pluginCtrl == null) return;

			_pluginCtrl._duringLoadChange = true;
			_pluginCtrl._triggerSlots.Clear();
			_pluginCtrl._queueSlots.Clear();
			_pluginCtrl._rulesCache.Clear();
		}

		internal static void OnDataApply(MaterialEditorCharaController _pluginCtrl)
		{

			AAAPKController _aaapkCtrl = GetController(_pluginCtrl.ChaControl);
			if (_pluginCtrl == null) return;

			_aaapkCtrl.ApplyParentRuleList("OnDataApply");
		}
	}
}
