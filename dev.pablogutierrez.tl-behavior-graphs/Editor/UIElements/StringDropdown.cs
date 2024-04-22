using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public delegate void AddParameterButtonClickedHandler();
public class StringDropdown : VisualElement
{
    // --------------------------------------------------------------------
    //                      Eventos expuestos
    // --------------------------------------------------------------------
    public event AddParameterButtonClickedHandler AddParameterButtonClicked;

    // --------------------------------------------------------------------
    //                      Constantes estáticas de la clase
    // --------------------------------------------------------------------
    public static readonly string NO_SELECTION = "Select a parameter";

    // --------------------------------------------------------------------
    //                      Propiedades Privadas de la clase
    // --------------------------------------------------------------------
    #region Propiedades Privadas de la Clase
    private Label label;
    private VisualElement container;
    private PopupField<string> popupField;
    private Button addParameterButton;
    private EventCallback<ChangeEvent<string>> selectionChangedCallback;
    #endregion

    // --------------------------------------------------------------------
    //                      Propiedades Públicas de la clase
    // --------------------------------------------------------------------
    public string Selection { get { return popupField.value; } }
    // --------------------------------------------------------------------
    //                            Ciclo de vida
    // --------------------------------------------------------------------
    #region Ciclo de vida
    public StringDropdown(string fieldName, List<string> options, EventCallback<ChangeEvent<string>> onSelectionChanged)
    {
        // Create a container for the dropdown
        container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        Add(container);

        // Create a label for the field name
        label = new Label(fieldName);
        container.Add(label);

        selectionChangedCallback = onSelectionChanged;
        UpdateOptions(options);
    } // constructor

    public void SetSelection(string selectedOption)
    {
        popupField.value = selectedOption;
    } // SetSelection

    public void UpdateOptions(List<string> options)
    {
        string previousSelection = NO_SELECTION;

        // Remove the existing popup field, if there was one
        if(popupField != null)
        {
            previousSelection = popupField.value;
            container.Remove(popupField);
        }

        // lo mismo para el botón de añadir nuevo parámetro
        if(addParameterButton != null)
        {
            container.Remove(addParameterButton);
        }

        List<string> newOptions = new List<string>(options);
        newOptions.Add(NO_SELECTION);

        // Create a new popup field with the updated options
        popupField = new PopupField<string>(newOptions, newOptions[newOptions.Count - 1]);
        popupField.RegisterValueChangedCallback(selectionChangedCallback);

        // restablecer la selección anterior si siguiera disponible en la lista de opciones
        if(newOptions.Contains(previousSelection))
        {
            popupField.value = previousSelection;
        }

        addParameterButton = new Button(ButtonSelected);
        addParameterButton.text = "+";

        // Add the new popup field to the container
        container.Add(popupField);
        container.Add(addParameterButton);
    } // UpdateOptions

    private void ButtonSelected()
    {
        AddParameterButtonClicked?.Invoke();
    } // ButtonSelected

    #endregion
} // StringDropdown
