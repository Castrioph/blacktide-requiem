using System.IO;
using UnityEditor;
using UnityEngine;
using BlacktideRequiem.Core.Data;
using BlacktideRequiem.Runtime.Demo;

namespace BlacktideRequiem.EditorTools
{
    /// <summary>
    /// Generates the S2-10 demo roster: 9 AbilityData + 3 CharacterData assets.
    /// Idempotent — overwrites fields on existing assets at the same paths.
    /// Data spec lives in DemoRosterFactory (shared with EditMode tests).
    /// </summary>
    public static class CreateDemoCharacterAssets
    {
        private const string ABILITIES_DIR = "Assets/Data/Abilities";
        private const string CHARACTERS_DIR = "Assets/Data/Characters";

        [MenuItem("Blacktide/Create Demo Character Assets")]
        public static void Create()
        {
            EnsureDir(ABILITIES_DIR);
            EnsureDir(CHARACTERS_DIR);

            var abilitySpecs = DemoRosterFactory.BuildAbilities();
            var persistedAbilities = new System.Collections.Generic.Dictionary<string, AbilityData>(abilitySpecs.Count);

            foreach (var pair in abilitySpecs)
            {
                string path = $"{ABILITIES_DIR}/{pair.Key}.asset";
                var persisted = PersistOrUpdate(path, pair.Value);
                persistedAbilities[pair.Key] = persisted;
            }

            var characterSpecs = DemoRosterFactory.BuildCharacters(persistedAbilities);

            foreach (var pair in characterSpecs)
            {
                string path = $"{CHARACTERS_DIR}/{pair.Key}.asset";
                PersistOrUpdate(path, pair.Value);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"S2-10: Created {abilitySpecs.Count} AbilityData + {characterSpecs.Count} CharacterData assets under {ABILITIES_DIR} and {CHARACTERS_DIR}.");
        }

        private static void EnsureDir(string assetPath)
        {
            string full = Path.Combine(Application.dataPath, "..", assetPath);
            if (!Directory.Exists(full))
                Directory.CreateDirectory(full);
        }

        /// <summary>
        /// If no asset exists at path, persist the given in-memory SO there.
        /// Otherwise copy fields from the spec onto the existing asset so
        /// references from scenes/prefabs stay intact.
        /// </summary>
        private static T PersistOrUpdate<T>(string path, T spec) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(spec, path);
                return spec;
            }

            EditorUtility.CopySerialized(spec, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(spec);
            return existing;
        }
    }
}
