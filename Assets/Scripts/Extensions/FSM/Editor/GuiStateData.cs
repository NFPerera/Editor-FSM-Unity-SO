using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.FSM.Base.StateMachine;
using UnityEngine;

namespace Assets.Scripts.Extensions.FSM.Editor
{
    public class GuiStateData
    {
        public GuiStateData(StateData p_data, string p_name, Rect p_expandedRect,Rect p_collapsedRect, MyState p_state, List<StateCondition> p_conditions, List<StateData> p_connections)
        {
            AssociatedStateData = p_data;
            StateName = p_name;
            ExpandedRect = p_expandedRect;
            CollapsedRect = p_collapsedRect;
            State = p_state;
            StateCondition = p_conditions;
            ExitStates = p_connections;
            GuiConnections = new List<GuiStateData>();
            UniqueId = Guid.NewGuid().ToString();
            IsExpanded =  true;
        }
        
        public Rect CurrentRect => IsExpanded ? ExpandedRect : CollapsedRect;
        public Rect ExpandedRect;
        public Rect CollapsedRect;

        public string StateName { get; set; }
        public StateData AssociatedStateData { get; set; }
        public MyState State { get; set; }
        public List<StateCondition> StateCondition { get; set; }
        public List<StateData> ExitStates { get; set; }
        public List<GuiStateData> GuiConnections { get; set; }
        public bool IsBeingDragged { get; set; }
        public bool IsExpanded  { get; set; }
        public string UniqueId { get; private set; }

        public void SetGuiConnections(List<GuiStateData> p_guiConnections)
        {
            GuiConnections = p_guiConnections;
        }

        public bool AddGuiConnections(GuiStateData p_guiConnection, StateCondition p_condition)
        {
            // Verificar si ya existe una conexión a este nodo
            if (GuiConnections.Contains(p_guiConnection))
            {
                Debug.LogWarning($"Ya existe una conexión al nodo {p_guiConnection.StateName}");
                return false;
            }

            GuiConnections.Add(p_guiConnection);
            StateCondition.Add(p_condition);
            ExitStates.Add(p_guiConnection.AssociatedStateData);
            return true;
        }
        
        public void UpdateExpandedRect(Rect p_newRect)
        {
            ExpandedRect = p_newRect;
        }

        public void UpdateCollapsedRect(Rect p_newRect)
        {
            CollapsedRect = p_newRect;
        }

        public void SetPosition(Vector2 position)
        {
            ExpandedRect.position = position;
            CollapsedRect.position = position;
        }
    }
}