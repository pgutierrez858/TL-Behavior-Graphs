using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace TLTLPredicateBuilder.Utils
{
    using Elements;
    using UnityEditor;
    using UnityEngine;

    public static class TLTLElementUtils
    {
        public static void PopulateNodeTitleContainer(VisualElement titleContainer, string titleLabel, Color color, string pathToIcon = null)
        {
            titleContainer.style.display = DisplayStyle.Flex;
            titleContainer.style.flexDirection = FlexDirection.Row;
            titleContainer.style.height = 50;

            // Special Header for node
            titleContainer.style.borderTopColor = color;
            titleContainer.style.borderTopWidth = 8;

            int insertIndex = 0;

            if(pathToIcon != null)
            {
                // Create an Image element for the icon
                Texture2D iconTexture = EditorGUIUtility.Load($"Packages/dev.pablogutierrez.tl-behavior-graphs/Editor/Images/{pathToIcon}") as Texture2D;
                Image iconImage = new Image();
                iconImage.image = iconTexture;

                VisualElement iconContainer = new VisualElement();
                iconContainer.style.height = 28;
                iconContainer.style.width = 28;
                iconContainer.style.marginLeft = 8;
                iconContainer.style.marginBottom = 8;
                iconContainer.style.marginTop = 8;
                iconContainer.style.alignSelf = Align.Center;
                iconContainer.Insert(0, iconImage);

                // add image to container
                titleContainer.Insert(insertIndex, iconContainer);
                insertIndex++;
            }
            
            Label returnNodeNameLabel = new Label(titleLabel);
            returnNodeNameLabel.style.fontSize = 14;
            returnNodeNameLabel.style.marginLeft = 4;
            returnNodeNameLabel.style.alignSelf = Align.Center;

            returnNodeNameLabel.AddClasses(
                "tltl-predicate-node__text-field"
            );

            titleContainer.Insert(insertIndex, returnNodeNameLabel);
        }  // PopulateNodeTitleContainer

        public static Button CreateButton(string text, Action onClick = null)
        {
            Button button = new Button(onClick)
            {
                text = text
            };

            return button;
        } // CreateButton

        public static Foldout CreateFoldout(string title, bool collapsed = false)
        {
            Foldout foldout = new Foldout()
            {
                text = title,
                value = !collapsed /* si value de un Foldout está en true, es porque NO está colapsado, y viceversa */
            };

            return foldout;
        } // CreateFoldout

        public static Port CreatePort(this TLTLPredicateNode node, Type portType, string portName = "", Orientation orientation = Orientation.Horizontal, Direction direction = Direction.Output, Port.Capacity capacity = Port.Capacity.Single)
        {
            Port port = node.InstantiatePort(orientation, direction, capacity, portType);

            port.portName = portName;

            return port;
        } // CreatePort

        public static TextField CreateTextField(string value = null, string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            TextField textField = new TextField()
            {
                value = value,
                label = label
            };

            if(onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            return textField;
        } // CreateTextField

        public static TextField CreateTextArea(string value = null, string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            TextField textArea = CreateTextField(value, label, onValueChanged);
            textArea.multiline = true;
            return textArea;
        } // CreateTextArea

    } // TLTLElementUtils

} // namespace TLTLPredicateBuilder.Utils
