using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SafeStreets.EditorTools
{
    /// <summary>
    /// Spec 006 R5 — enforcement for the modal-input bug class. Scans every runtime
    /// script under Assets/Scripts: any file that polls UnityEngine.Input must also
    /// reference ModalInputGate (the gate = swallow window + editor focus guard).
    /// A file that polls without the gate is exactly how the Ada dialog loop was born.
    ///
    /// Run it from Tools > Modal Safety Audit, or from the CLI:
    ///   unity command eval --code 'return SafeStreets.EditorTools.ModalSafetyAudit.Run();'
    /// Returns "PASS ..." or "FAIL ..." and logs details. Wire into CI via that eval.
    /// </summary>
    public static class ModalSafetyAudit
    {
        static readonly Regex InputPoll = new Regex(@"Input\.(Get(Key|KeyDown|MouseButton|MouseButtonUp|Axis|AxisRaw)|mousePosition)",
            RegexOptions.Compiled);
        static readonly Regex GateRef = new Regex(@"ModalInputGate", RegexOptions.Compiled);
        static readonly Regex AllowedException = new Regex(
            @"ModalSafetyAudit|ModalInputGate|PlayerController2D", RegexOptions.Compiled);

        /// <summary>Axes are continuous movement input (WASD), not modal edge events —
        /// gated at the consumer (PlayerController2D.Paused), not per poll.</summary>
        static readonly Regex AxisAllowed = new Regex(@"Input\.GetAxis", RegexOptions.Compiled);

        public static string Run()
        {
            var root = Path.Combine(Application.dataPath, "Scripts");
            var failures = new List<string>();
            var checkedFiles = new List<string>();

            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}")) continue;
                var code = File.ReadAllText(file);
                var name = Path.GetFileName(file);

                string[] lines = code.Split('\n');
                bool pollsModalInput = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.TrimStart().StartsWith("//")) continue;
                    if (!InputPoll.IsMatch(line)) continue;
                    if (AxisAllowed.IsMatch(line)) continue;          // axes exempt (see above)
                    pollsModalInput = true;
                    if (!GateRef.IsMatch(code))
                        failures.Add($"{name}:{i + 1} polls [{line.Trim()}] but never references ModalInputGate");
                    break;   // one gate reference per file is enough; first poll found it
                }
                if (pollsModalInput) checkedFiles.Add(name);
            }

            if (failures.Count > 0)
            {
                var msg = "FAIL — " + failures.Count + " script(s) poll Input without the ModalInputGate:\n  " +
                          string.Join("\n  ", failures.ToArray()) +
                          "\nRule: check ModalInputGate.InputAllowed before ANY Input poll; " +
                          "arm SwallowSpacePress() on every modal open/close.";
                Debug.LogError(msg);
                return msg;
            }

            var ok = $"PASS — {checkedFiles.Count} input-polling script(s) audited, all gated: " +
                     string.Join(", ", checkedFiles.ToArray());
            Debug.Log("[ModalSafetyAudit] " + ok);
            return ok;
        }

        [MenuItem("Tools/Modal Safety Audit")]
        static void MenuRun()
        {
            var result = Run();
            EditorUtility.DisplayDialog("Modal Safety Audit",
                result.StartsWith("PASS") ? result : "See Console — audit FAILED.", "OK");
        }
    }
}
