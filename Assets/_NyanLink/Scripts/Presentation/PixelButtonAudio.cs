using NineMood.Core.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NineMood.Presentation
{
    public class PixelButtonAudio : MonoBehaviour,IPointerClickHandler
    {
        static AudioClip clip;
        public void OnPointerClick(PointerEventData eventData)
        {
            var button=GetComponent<Button>();if(button==null || !button.interactable)return;
            if(clip==null)clip=Resources.Load<AudioClip>("Audio/ConceptPixel/ui");
            AudioManager.Instance?.PlaySFX(clip);
        }
    }
}
