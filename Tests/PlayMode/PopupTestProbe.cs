using System;
using Popup;

namespace Song.UnityPopup.Tests {
    public class PopupTestProbe : PopupHandler {
        public class Options : Param {
            public bool CloseImmediately;
            public bool ThrowOnSetup;
            public bool BlockEscape;
        }

        private bool blockEscape;
        public override bool CanCloseWithEsc => !blockEscape;

        public override void SetData(Param param, int sortingOrder) {
            base.SetData(param, sortingOrder);
            var options = (Options)param;
            blockEscape = options.BlockEscape;
            if (options.ThrowOnSetup) throw new InvalidOperationException("Expected setup failure");
            if (options.CloseImmediately) RequestPopupClosing();
        }
    }
}
