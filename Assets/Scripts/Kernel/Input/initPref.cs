

// using System.Collections;
// using Lonize.Logging;
// using Unity.Mathematics;
// using UnityEngine;



// namespace Kernel
// {
//     public class InitPref : MonoBehaviour
//     {

//         public IEnumerator Start()
//         {
//             SaveControlCommand pref = new SaveControlCommand();
//             yield return new WaitForSeconds(0.5f);
//             pref.ApplyControlPrefsFromSave();
//             // foreach (var kv in InputConfiguration.ControlCommand)
//             // {
//             //     Log.Info($"Current Pref - Key: {kv.Key}, Value: {kv.Value}");
//             // }
//             // Log.Info(pref.ControlCommand["Up"].ToString());
//             // Log.Info(pref.ControlCommand);
//             foreach (var kv in InputConfiguration.ControlCommand)
//             {
//                 Log.Info($"Key: {kv.Key}, Value: {kv.Value}");
//                 // Log.Info(kv.ToString());
//             }
//         }
//     }
// }