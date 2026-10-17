using UnityEditor;
using UnityEngine;
namespace ToyEconomy.Editor {
[InitializeOnLoad]
public static class VerificationHook {
 static VerificationHook(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ToyEconomy.Verify",false)){SessionState.SetBool("ToyEconomy.Verify",false);new GameObject("MVP Verification").AddComponent<ToyEconomyVerification>();}};}
}
}
