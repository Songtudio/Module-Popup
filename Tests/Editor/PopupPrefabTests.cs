using System.Linq;
using NUnit.Framework;
using Popup;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Song.UnityPopup.Tests {
    public class PopupPrefabTests {
        [TestCase("OneButtonPopup", typeof(OneButtonPopup))]
        [TestCase("TwoButtonPopup", typeof(TwoButtonPopup))]
        public void BundledPrefabHasCompleteBindings(string name, System.Type expected) {
            var popup = Resources.Load<PopupHandler>(name);
            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.GetType(), Is.EqualTo(expected));
            Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(popup.gameObject), Is.Zero);
            var serialized = new SerializedObject(popup);
            foreach (var field in new[] { "canvas", "titleText", "descriptionText", "quitButton", "okButton" }) {
                Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            }
            if (popup is TwoButtonPopup) {
                Assert.That(serialized.FindProperty("noButton").objectReferenceValue, Is.Not.Null);
            }
            foreach (var text in popup.GetComponentsInChildren<TextMeshProUGUI>(true)) {
                Assert.That(text.font, Is.Not.Null, text.name);
                Assert.That(text.font.name, Does.StartWith("LiberationSans"), text.name);
                Assert.That(text.fontSharedMaterial, Is.Not.Null, text.name);
                Assert.That(text.fontSharedMaterial.shader, Is.Not.Null, text.name);
                Assert.That(text.fontSharedMaterial.shader.isSupported, Is.True, text.name);
            }
            foreach (var image in popup.GetComponentsInChildren<UnityEngine.UI.Image>(true)) {
                Assert.That(image.sprite, Is.Null, "Generic UI must not require project artwork: " + image.name);
            }
            foreach (var dependency in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(popup), true)) {
                Assert.That(AssetDatabase.LoadMainAssetAtPath(dependency), Is.Not.Null, dependency);
                Assert.That(dependency.StartsWith("Assets/"), Is.False,
                    "Package prefab must not require a host-project asset: " + dependency);
            }
        }

        [Test]
        public void RuntimeAssemblyDoesNotReferenceGameOrVisualScripting() {
            var references = typeof(PopupManager).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
            Assert.That(references, Does.Not.Contain("Assembly-CSharp"));
            Assert.That(references.Any(n => n.StartsWith("Unity.VisualScripting")), Is.False);
            Assert.That(typeof(PopupHandler).Assembly.GetName().Name, Is.EqualTo("Song.UnityPopup"));
        }

        [TestCase("CharacterPopup")]
        [TestCase("ModePopup")]
        [TestCase("MatchingPopup")]
        [TestCase("CountdownPopup")]
        public void CrawlStarsVariantsRetainTheirComponentBindings(string name) {
            string path = "Assets/Prefabs/Popups/Resources/" + name + ".prefab";
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) Assert.Ignore("CrawlStars integration check only; this is another host project.");
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) {
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero, child.name);
            }
            var popup = root.GetComponent<PopupHandler>();
            Assert.That(popup, Is.Not.Null, name);
            var serialized = new SerializedObject(popup);
            Assert.That(serialized.FindProperty("canvas").objectReferenceValue, Is.Not.Null, name);
            if (name == "CharacterPopup" || name == "ModePopup") {
                Assert.That(serialized.FindProperty("titleText").objectReferenceValue, Is.Not.Null, name);
                Assert.That(serialized.FindProperty("quitButton").objectReferenceValue, Is.Not.Null, name);
                Assert.That(serialized.FindProperty("okButton").objectReferenceValue, Is.Not.Null, name);
            }
            Assert.That(AssetDatabase.GetDependencies(path, true),
                Does.Contain("Packages/com.song.unity-popup/Runtime/Resources/BasePopup.prefab"));
        }
    }
}
