using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Popup {
    public class PopupManager {
        private static PopupManager instance;
        public static PopupManager Instance => instance ??= new PopupManager();

        private readonly List<KeyValuePair<string, PopupHandler>> popupList =
            new List<KeyValuePair<string, PopupHandler>>();
        
        public bool HasOpenedPopup => popupList.Count > 0;
        public bool CanCloseTopWithEsc => HasOpenedPopup && popupList[^1].Value.CanCloseWithEsc;

        public async UniTask<PopupHandler.Result> ShowAsync(string name, PopupHandler.Param param = null) {
            var resource = Resources.Load<PopupHandler>(name);
            if (resource == null) {
                Debug.LogError($"PopupManager.Show::{name} 프리팹을 찾을 수 없습니다.");
                return null;
            }

            var obj = Object.Instantiate(resource);
            popupList.Add(new KeyValuePair<string, PopupHandler>(name, obj));
            // A middle popup can be closed, so Count * 1000 can reuse the top order.
            int sortingOrder = 1000;
            foreach (var popup in popupList) {
                if (popup.Value != obj) {
                    sortingOrder = Mathf.Max(sortingOrder, popup.Value.SortingOrder + 1000);
                }
            }

            try {
                // Capture before SetData: a derived popup can synchronously close itself.
                var closeTask = obj.WaitForCloseAsync();
                obj.SetData(param, sortingOrder);
                if (popupList.Exists(popup => popup.Value == obj)) {
                    obj.gameObject.SetActive(true);
                }
                return await closeTask;
            } catch {
                int index = popupList.FindIndex(popup => popup.Value == obj);
                if (index >= 0) RemovePopup(obj, index);
                throw;
            }
        }

        public void CloseTop() {
            if (popupList.Count == 0) {
                Debug.LogError($"PopupManager.Close::닫을 팝업이 없습니다.");
                return;
            }

            RemovePopup(popupList[^1].Value, popupList.Count - 1);
        }

        public void CloseByName(string name) {
            // 보통 Top 부터 닫는 경우가 많아서 역으로 순회
            for (int i = popupList.Count - 1; i >= 0; --i) {
                var popup = popupList[i];
                if (popup.Key == name) {
                    RemovePopup(popup.Value, i);
                    return;
                }
            }

            Debug.LogError($"PopupManager.CloseByName::{name}이 열려있지 않습니다.");
        }

        public void Close(PopupHandler target, PopupHandler.Result result) {
            // 보통 Top 부터 닫는 경우가 많아서 역으로 순회
            for (int i = popupList.Count - 1; i >= 0; --i) {
                var popup = popupList[i];
                if (popup.Value == target) {
                    RemovePopup(popup.Value, i, result);
                    return;
                }
            }

            Debug.LogError($"PopupManager.Close::{(target != null ? target.name : "null")}이 열려있지 않습니다.");
        }

        public void CloseAll() {
            for (int i = popupList.Count - 1; i >= 0; --i) {
                RemovePopup(popupList[i].Value, i);
            }
        }

        private void RemovePopup(PopupHandler target, int listIdx, PopupHandler.Result result = null) {
            popupList.RemoveAt(listIdx);
            if (target == null) return;
            target.gameObject.SetActive(false);
            try {
                target.Dispose(result);
            } finally {
                Object.Destroy(target.gameObject);
            }
        }

        internal void NotifyDestroyed(PopupHandler target) {
            popupList.RemoveAll(popup => ReferenceEquals(popup.Value, target));
        }
    }
}
