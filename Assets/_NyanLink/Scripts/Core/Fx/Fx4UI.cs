using System;
using System.Collections.Generic;
using NineMood.Core.Fx;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Fx4UI : FxCore
{
    #region Sources
    [Serializable]
    public class ImageSource : BaseSource
    {
        public Image image;
    }

    [Serializable]
    public class TextSource : BaseSource
    {
        public Text text;
    }

    [Serializable]
    public class TextMeshProUGUISource : BaseSource
    {
        public TextMeshProUGUI textMeshProUGUI;
    }

    [SerializeField, HideInInspectorCustom] protected List<ImageSource> images;
    [SerializeField, HideInInspectorCustom] protected List<TextSource> texts;
    [SerializeField, HideInInspectorCustom] protected List<TextMeshProUGUISource> tmpList;

    [SerializeField, HideInInspector] private bool foldoutImages;
    [SerializeField, HideInInspector] private bool foldoutTexts;
    [SerializeField, HideInInspector] private bool foldoutTMPList;

    #endregion

    #region Animations


    #endregion
}
