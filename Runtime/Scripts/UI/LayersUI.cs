using Project;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Virgis
{

    /// <summary>
    /// LayersUI is the mediator for all components within the Layers UI GO (i.e. Layers Menu).
    /// </summary>
    /// 
    /// 
    public class LayersUI : MonoBehaviour
    {
        public GameObject layersScrollView;
        public GameObject layerPanelPrefab;
        public GameObject menus;

        private AppState _appState;
        private Dictionary<Guid, LayerUIPanel> _layersMap;
        private IDisposable startsub;
        private IDisposable stopsub;
        private IDisposable layersub;

        // Start is called before the first frame update
        void Start()
        {
            _appState = AppState.instance;
            startsub = _appState.editSession.StartEvent.Subscribe(OnStartEditSession);
            stopsub = _appState.editSession.EndEvent.Subscribe(OnEndEditSession);
            layersub = _appState.LayerUpdate.Event.Subscribe(onLayerUpdate);
            _layersMap = new Dictionary<Guid, LayerUIPanel>();
            CreateLayerPanels();
        }

        private void OnDestroy() {
            startsub.Dispose();
            stopsub.Dispose();
            layersub.Dispose();
        }

        public void OnShowMenuButtonClicked()
        {
            gameObject.SetActive(false);
            menus.SetActive(true);
        }

        public void CreateLayerPanels()
        {
            // Delete any existing panel
            foreach (var panel in _layersMap) {
                Destroy(panel.Value.gameObject);
            }
            _layersMap.Clear();

            // appState.layers are actually Layer script (Component)
            AppState.instance.layers.ForEach(comp =>
            {
                IVirgisLayer layer = (IVirgisLayer)comp;
                Debug.Log($"CreateLayerPanels: layer {layer.GetMetadata().Id ?? ""}, {layer.GetMetadata().DisplayName ?? ""}");
                // create a view panel for this particular layer
                GameObject newLayerPanel = Instantiate(layerPanelPrefab, transform);
                // obtain the panel script
                LayerUIPanel panelScript = newLayerPanel.GetComponentInChildren<LayerUIPanel>();
                LayerUIContainer containerScript = newLayerPanel.GetComponentInChildren<LayerUIContainer>();
                containerScript._layersMap = _layersMap;
                // set the layer in the panel
                containerScript.layer = layer;

                containerScript.viewLayerToggle.isOn = layer.IsVisible();

                _layersMap.Add(Guid.NewGuid(), panelScript);
                newLayerPanel.transform.SetParent(layersScrollView.transform, false);
            });
            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

        private void onLayerUpdate(IVirgisLayer layer) {
            CreateLayerPanels();
        }

        private void OnStartEditSession(bool ignore)
        {
            foreach (LayerUIPanel panel in _layersMap.Values)
            {
                if (panel.editLayerToggle != null)
                    panel.editLayerToggle.interactable = true;
            }
        }

        private void OnEndEditSession(bool saved)
        {
            foreach (LayerUIPanel panel in _layersMap.Values)
            {
                if (panel.editLayerToggle != null)
                    panel.editLayerToggle.interactable = false;
            }
        }

        private void printEditStatus() {
            string msg = "edit status: ";
            foreach (LayerUIPanel l in _layersMap.Values) {
                msg += $"({l.layer.GetMetadata().Id}: {l.layer.IsEditable()}) ";
            }
            Debug.Log(msg);
        }
    }
}