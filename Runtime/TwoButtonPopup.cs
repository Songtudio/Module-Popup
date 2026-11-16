using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Popup {
    [MovedFrom(true, "Popup", "Assembly-CSharp", "TwoButtonPopup")]
    public class TwoButtonPopup : CommonPopup {
        public class Result : PopupHandler.Result {
            public bool isClickedOk;
            public Result(bool isClickedOk) {
                this.isClickedOk = isClickedOk;
            }
        }

        [SerializeField] protected UnityEngine.UI.Button noButton;

        protected override void Start() {
            base.Start();
            if (noButton != null) noButton.onClick.AddListener(OnClickNo);
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            if (noButton != null) noButton.onClick.RemoveListener(OnClickNo);
        }

        protected override void OnClickQuit() {
            RequestPopupClosing(new Result(false));
        }

        protected override void OnClickOk() {
            RequestPopupClosing(new Result(true));
        }

        protected void OnClickNo() {
            RequestPopupClosing(new Result(false));
        }
    }
}
