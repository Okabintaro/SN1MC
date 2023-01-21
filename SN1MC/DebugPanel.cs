using HarmonyLib;
using UnityEngine;
using TMPro;

class DebugPanel : MonoBehaviour {

    TextMeshProUGUI entry;
    static DebugPanel main;

    void Start() {
        var prefabMessage = ErrorMessage.main.prefabMessage;
        GameObject obj = Object.Instantiate(prefabMessage);
        entry = obj.GetComponent<TextMeshProUGUI>();
        entry.rectTransform.SetParent(ErrorMessage.main.messageCanvas, false);
        obj.SetActive(true);
        entry.text = "";

        main = this;
    }

    public static void Show(string message) {
        if (DebugPanel.main == null) {
            return;
        }
        DebugPanel.main.entry.text = message;
    }

    // There might be a better hook for this
    [HarmonyPatch(typeof(uGUI), nameof(uGUI.Awake))]
    public static class uGUI_CreateDebug
    {
        [HarmonyPostfix]
        public static void Postfix(uGUI_MainMenu __instance)
        {
            __instance.gameObject.AddComponent<DebugPanel>();
        }
    }

}