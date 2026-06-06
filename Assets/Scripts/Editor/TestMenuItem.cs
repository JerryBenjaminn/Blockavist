using UnityEditor;
using UnityEngine;

public static class TestMenuItem
{
    [MenuItem("Cubby's Blocks/Test")]
    public static void Test()
    {
        Debug.Log("Claude Code is working");
    }

    [MenuItem("Cubby's Blocks/Debug/Clear PlayerPrefs")]
    public static void ClearPlayerPrefs()
    {
        // DeleteAll() may target a different store when called from Edit mode on some
        // platforms. Explicitly delete every known key so it works in all contexts.
        PlayerPrefs.DeleteKey("Tutorial_L1_Seen");
        PlayerPrefs.DeleteKey("unlockedLevel");
        PlayerPrefs.DeleteKey("musicEnabled");
        PlayerPrefs.DeleteKey("sfxEnabled");
        PlayerPrefs.Save();

        // Read back immediately to confirm the store was actually cleared.
        int tutorial  = PlayerPrefs.GetInt("Tutorial_L1_Seen", 0);
        int unlocked  = PlayerPrefs.GetInt("unlockedLevel",    0);
        Debug.Log($"[Cubby's Blocks] PlayerPrefs cleared. Verify — Tutorial_L1_Seen={tutorial}  unlockedLevel={unlocked}  (both should be 0)");
    }
}
