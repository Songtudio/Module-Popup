using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Popup;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Song.UnityPopup.Tests {
    public class PopupLifecycleTests {
        private PopupManager Manager => PopupManager.Instance;

        [UnitySetUp]
        public IEnumerator SetUp() {
            Manager.CloseAll();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown() {
            Manager.CloseAll();
            yield return null;
        }

        private static CommonPopup.Param Message(string title = "Title") =>
            new CommonPopup.Param(title, "Description");

        private static T[] Opened<T>() where T : PopupHandler =>
            UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);

        private static UnityEngine.UI.Button Button(PopupHandler popup, string name) =>
            (UnityEngine.UI.Button)typeof(PopupHandler).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);

        private static Canvas PopupCanvas(PopupHandler popup) =>
            (Canvas)typeof(PopupHandler).GetField("canvas",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);

        [UnityTest]
        public IEnumerator ShowLoadsBundledPrefabAndClosesWithResult() {
            var task = Manager.ShowAsync("TwoButtonPopup", Message());
            yield return null; // Button listeners are attached in Start, as in CrawlStars.
            var popup = Opened<TwoButtonPopup>().Single();
            Assert.That(Manager.HasOpenedPopup, Is.True);
            Assert.That(Manager.CanCloseTopWithEsc, Is.True);
            Assert.That(PopupCanvas(popup).sortingOrder, Is.EqualTo(1000));
            Assert.That(popup.GetComponentsInChildren<TextMeshProUGUI>().Any(t => t.text == "Title"), Is.True);
            Button(popup, "okButton").onClick.Invoke();
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(((TwoButtonPopup.Result)task.GetAwaiter().GetResult()).isClickedOk, Is.True);
            Assert.That(Manager.HasOpenedPopup, Is.False);
        }

        [UnityTest]
        public IEnumerator QuitAndNoProduceNegativeResult() {
            foreach (bool useQuit in new[] { true, false }) {
                var task = Manager.ShowAsync("TwoButtonPopup", Message());
                yield return null;
                var popup = Opened<TwoButtonPopup>().Single();
                var button = useQuit ? Button(popup, "quitButton") :
                    (UnityEngine.UI.Button)typeof(TwoButtonPopup).GetField("noButton",
                        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);
                button.onClick.Invoke();
                Assert.That(((TwoButtonPopup.Result)task.GetAwaiter().GetResult()).isClickedOk, Is.False);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator OneButtonCompletesWithNull() {
            var task = Manager.ShowAsync("OneButtonPopup", Message());
            yield return null;
            Button(Opened<OneButtonPopup>().Single(), "okButton").onClick.Invoke();
            Assert.That(task.GetAwaiter().GetResult(), Is.Null);
        }

        [UnityTest]
        public IEnumerator CloseTopAndCloseByNameUseLastOpenedInstance() {
            var first = Manager.ShowAsync("OneButtonPopup", Message("first"));
            var second = Manager.ShowAsync("OneButtonPopup", Message("second"));
            var third = Manager.ShowAsync("TwoButtonPopup", Message("third"));
            Manager.CloseByName("OneButtonPopup");
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(first.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(third.Status, Is.EqualTo(UniTaskStatus.Pending));
            Manager.CloseTop();
            Assert.That(third.GetAwaiter().GetResult(), Is.Null);
            Assert.That(first.Status, Is.EqualTo(UniTaskStatus.Pending));
            Manager.CloseAll();
            Assert.That(first.GetAwaiter().GetResult(), Is.Null);
            Assert.That(second.GetAwaiter().GetResult(), Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosingMiddleDoesNotReuseTopSortingOrder() {
            var first = Manager.ShowAsync("OneButtonPopup", Message());
            var second = Manager.ShowAsync("TwoButtonPopup", Message());
            Manager.CloseByName("OneButtonPopup");
            var third = Manager.ShowAsync("OneButtonPopup", Message());
            Assert.That(PopupCanvas(Opened<OneButtonPopup>().Single(p => p.gameObject.activeSelf))
                .sortingOrder, Is.EqualTo(3000));
            Manager.CloseAll();
            first.GetAwaiter().GetResult();
            second.GetAwaiter().GetResult();
            third.GetAwaiter().GetResult();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExternalDestructionSettlesWaiterAndRemovesEntry() {
            var task = Manager.ShowAsync("OneButtonPopup", Message());
            UnityEngine.Object.Destroy(Opened<OneButtonPopup>().Single().gameObject);
            yield return null;
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(task.GetAwaiter().GetResult(), Is.Null);
            Assert.That(Manager.HasOpenedPopup, Is.False);
        }

        [UnityTest]
        public IEnumerator SynchronousSelfCloseDoesNotReactivateObject() {
            var task = Manager.ShowAsync("PopupTestProbe", new PopupTestProbe.Options { CloseImmediately = true });
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(Manager.HasOpenedPopup, Is.False);
            Assert.That(Opened<PopupTestProbe>(), Is.Empty);
            Assert.That(task.GetAwaiter().GetResult(), Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SetDataFailureRemovesPartiallyOpenedPopup() {
            var task = Manager.ShowAsync("PopupTestProbe", new PopupTestProbe.Options { ThrowOnSetup = true });
            Assert.Throws<InvalidOperationException>(() => task.GetAwaiter().GetResult());
            Assert.That(Manager.HasOpenedPopup, Is.False);
            yield return null;
            Assert.That(Opened<PopupTestProbe>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator EscapePolicyComesFromTopPopup() {
            var task = Manager.ShowAsync("PopupTestProbe", new PopupTestProbe.Options { BlockEscape = true });
            Assert.That(Manager.CanCloseTopWithEsc, Is.False);
            // CloseTop deliberately remains unconditional; the input owner checks policy.
            Manager.CloseTop();
            Assert.That(task.GetAwaiter().GetResult(), Is.Null);
            yield return null;
        }

        [Test]
        public void MissingResourceDoesNotCreateStackEntry() {
            LogAssert.Expect(LogType.Error, "PopupManager.Show::__MissingPopup__ 프리팹을 찾을 수 없습니다.");
            Assert.That(Manager.ShowAsync("__MissingPopup__").GetAwaiter().GetResult(), Is.Null);
            Assert.That(Manager.HasOpenedPopup, Is.False);
        }

        [Test]
        public void EmptyCloseAndUnknownNameHaveStableDiagnostics() {
            LogAssert.Expect(LogType.Error, "PopupManager.Close::닫을 팝업이 없습니다.");
            Manager.CloseTop();
            LogAssert.Expect(LogType.Error, "PopupManager.CloseByName::missing이 열려있지 않습니다.");
            Manager.CloseByName("missing");
            LogAssert.Expect(LogType.Error, "PopupManager.Close::null이 열려있지 않습니다.");
            Manager.Close(null, null);
        }
    }
}
