using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PalmMenu : MonoBehaviour
{
    [SerializeField] private SunAngleCalculator _sunAngleCalculator;
    [SerializeField] private SunAngleInput _sunAngleInput;
    [SerializeField] private List<MeshRenderer> _datePickerObjects;
    [SerializeField] [Range(0.1f, 1.0f)] private float _interactableTransitionTime = 0.5f;

    [Header("Buttons")] 
    [SerializeField] private TMP_Text _dateButtonText;
    [SerializeField] private TMP_Text _animateButtonText;
    
    private bool _dateObjectsOn = true;
    private bool _sunAnimationPlayed = false;

    public void ToggleDateObjectDisplay()
    {
        if (_dateObjectsOn)
        {
            // Right now this list of objects is pretty large, about 48. A potential
            // improvement, in terms of making it easier to manage the scene, would
            // be to just assign parent objects to the list field, then use
            // GetComponentsInChildren here. But I need to move on...
            foreach (var obj in _datePickerObjects)
                StartCoroutine(ObjectFader(obj, false));
            _dateButtonText.text = "Set Date";
            _dateObjectsOn = false;
        }
        else
        {
            foreach (var obj in _datePickerObjects)
                StartCoroutine(ObjectFader(obj, true));
            _dateButtonText.text = "Hide Date Controls";
            _dateObjectsOn = true;
            _sunAngleInput.MakeOnlyCurrentDateActive();
        }
    }

    public void AnimateTheSunOrReset()
    {
        if (_sunAnimationPlayed)
        {
            _animateButtonText.text = "Animate the Day";
            _sunAnimationPlayed = false;
            _sunAngleCalculator.ApplySolarValuesToSunLight();
        }
        else
        {
            _animateButtonText.text = "Reset the Sun";
            _sunAnimationPlayed = true;
            _sunAngleCalculator.AnimateTheDay();
        }
    }

    private IEnumerator ObjectFader(MeshRenderer obj, bool fadeIn)
    {
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;

        if (fadeIn)
            obj.gameObject.SetActive(true);
        
        Material mtl = obj.material;
        float diffAlpha = (endAlpha - mtl.color.a);

        float counter = 0;
        while (counter < _interactableTransitionTime)
        {
            // Unsure if it would be more efficient to set up the post-fade color, then
            // use Color.Lerp here. No time to test it out...
            float alphaAmount = mtl.color.a + (Time.deltaTime * diffAlpha) / _interactableTransitionTime;
            mtl.color = new Color(mtl.color.r, mtl.color.g, mtl.color.b, alphaAmount);
            counter += Time.deltaTime;
            yield return null;
        }

        mtl.color = new Color(mtl.color.r, mtl.color.g, mtl.color.b, endAlpha);
        if (!fadeIn)
            obj.gameObject.SetActive(false);
    }
}
