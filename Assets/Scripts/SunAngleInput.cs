using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SunAngleInput : MonoBehaviour
{
    [SerializeField] private SunAngleCalculator _sunAngleCalculator;
    [SerializeField] private Transform _monthCylinderTransform;
    //[SerializeField] private GameObject _dateDecreaseButton;
    //[SerializeField] private GameObject _dateIncreaseButton;
    [SerializeField] private List<GameObject> _dateObjects;

    // Start and end of a single interaction
    private Vector3 _monthWheelStartEuler;
    private Vector3 _monthWheelEndEuler;
    private int _datePickerCurrentDate = 1;
    
    // Month Wheel
    public void MonthWheelSelectEntered(SelectEnterEventArgs args)
    {
        _monthWheelStartEuler = _monthCylinderTransform.localEulerAngles;
        //Debug.Log($"Enter Local Euler {_dateWheelStartEuler}");
        Months month = ConvertAngleToMonth(_monthWheelStartEuler);
        //Debug.Log($"Month starts at {month.ToString()}");
    }
    public void MonthWheelSelectExited(SelectExitEventArgs args)
    {
        _monthWheelEndEuler = _monthCylinderTransform.localEulerAngles;
        //Debug.Log($"Exit Local Euler {_dateWheelEndEuler}");
        Months month = ConvertAngleToMonth(_monthWheelEndEuler);
        //Debug.Log($"Month NOW set to {month.ToString()}");
        _sunAngleCalculator.month = month;
        _sunAngleCalculator.ApplySolarValuesToSunLight();
    }
    private Months ConvertAngleToMonth(Vector3 dateWheelLocalEuler)
    {
        // Just using the values as they are in the model; November is 0, December is 330
        int snappedAngle = Mathf.RoundToInt(dateWheelLocalEuler.y);
        Months month = Months.June;
        switch (snappedAngle)
        {
            case 0: month = Months.November; break;
            case 30: month = Months.October; break;
            case 60: month = Months.September; break;
            case 90: month = Months.August; break;
            case 120: month = Months.July; break;
            case 150: month = Months.June; break;
            case 180: month = Months.May; break;
            case 210: month = Months.April; break;
            case 240: month = Months.March; break;
            case 270: month = Months.February; break;
            case 300: month = Months.January; break;
            case 330: month = Months.December; break;
        }
        // need some fallback
        return month;
    }
    
    // Date Picker
    public void DateDownButtonOnRelease()
    {
        if (_datePickerCurrentDate <= 1)
            return;
        _dateObjects[_datePickerCurrentDate-1].SetActive(false);
        _datePickerCurrentDate--;
        _dateObjects[_datePickerCurrentDate-1].SetActive(true);
        _sunAngleCalculator.dayOfMonth = _datePickerCurrentDate;
        _sunAngleCalculator.ApplySolarValuesToSunLight();
    }
    public void DateUpButtonOnRelease()
    {
        int maxDate = MaxDateThisMonth();
        if (_datePickerCurrentDate >= maxDate)
            return;
        _dateObjects[_datePickerCurrentDate-1].SetActive(false);
        _datePickerCurrentDate++;
        _dateObjects[_datePickerCurrentDate-1].SetActive(true);
        _sunAngleCalculator.dayOfMonth = _datePickerCurrentDate;
        _sunAngleCalculator.ApplySolarValuesToSunLight();
    }
    private int MaxDateThisMonth()
    {
        switch (_sunAngleCalculator.month)
        {
            case Months.February: 
                return 28;
            case Months.April:
            case Months.June:
            case Months.September:
            case Months.November:
                return 30;
            default:
                return 31;
        }
    }
    public void MakeOnlyCurrentDateActive()
    {
        for (int i = 0; i < _dateObjects.Count; ++i)
        {
            if (i == _datePickerCurrentDate - 1)
                _dateObjects[i].SetActive(true);
            else
                _dateObjects[i].SetActive(false);
        }
    }


    // public void DateWheelOnValueChanged(float angle)
    // {
    //     Debug.Log($"{nameof(DateWheelOnValueChanged)}, angle {angle:0.0#####}");
    // }
    // public void DateWheelFirstHoverEntered(HoverEnterEventArgs args)
    // {
    //     Debug.Log($"{nameof(DateWheelFirstHoverEntered)}, interactable {args.interactableObject.transform.name}, interactor {args.interactorObject.transform.name}");
    // }
    // public void DateWheelLastHoverExited(HoverExitEventArgs args)
    // {
    //     Debug.Log($"{nameof(DateWheelLastHoverExited)}, interactable {args.interactableObject.transform.name}, interactor {args.interactorObject.transform.name}");
    // }
    // public void DateWheelHoverEntered(HoverEnterEventArgs args)
    // {
    //     Debug.Log($"{nameof(DateWheelHoverEntered)}, interactable {args.interactableObject.transform.name}, interactor {args.interactorObject.transform.name}");
    // }
    // public void DateWheelHoverExited(HoverExitEventArgs args)
    // {
    //     Debug.Log($"{nameof(DateWheelHoverExited)}, interactable {args.interactableObject.transform.name}, interactor {args.interactorObject.transform.name}");
    // }
    // public void DateWheelFirstSelectEntered(SelectEnterEventArgs args)
    // {
    //     Debug.Log($"{nameof(DateWheelFirstSelectEntered)}, interactable {args.interactableObject.transform.name}, interactor {args.interactorObject.transform.name}");
    // }
    // public void DateWheelLastSelectExited(SelectExitEventArgs args)
    // {
    //     Debug.Log($"{nameof(DateWheelLastSelectExited)}, interactable {args.interactableObject.transform.name}, interactor {args.interactorObject.transform.name}");
    // }
    // public void DateWheelActivated(ActivateEventArgs args)
    // {
    //     Debug.Log($"{nameof(DateWheelActivated)}, interactable {args.interactableObject.transform.name}, interactor {args.interactorObject.transform.name}");
    // }
    // public void DateWheelDeactivated(DeactivateEventArgs args)
    // {
    //     Debug.Log($"{nameof(DateWheelDeactivated)}, interactable {args.interactableObject.transform.name}, interactor {args.interactorObject.transform.name}");
    // }
}
