/*----------------------------------------------------------
This Source Code Form is subject to the terms of the
Mozilla Public License, v.2.0. If a copy of the MPL
was not distributed with this file, You can obtain one
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using OneScript.DebugProtocol;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;
using Variable = OneScript.DebugProtocol.Variable;
using MachineVariable = ScriptEngine.Machine.Variable;

namespace OneScript.DebugServices
{
    public class DefaultVariableVisualizer : IVariableVisualizer
    {
        public Variable GetVariable(IVariable value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            
            string presentation;
            string typeName;

            //На случай проблем, подобных:
            //https://github.com/EvilBeaver/OneScript/issues/918

            try
            {
                presentation = value.AsString();
            }
            catch (Exception e)
            {
                presentation = e.Message;
            }

            try
            {
                typeName = value.SystemType.Name;
            }
            catch (Exception e)
            {
                typeName = e.Message;
            }

            if (presentation.Length > DebuggerSettings.MAX_PRESENTATION_LENGTH)
                presentation = presentation.Substring(0, DebuggerSettings.MAX_PRESENTATION_LENGTH) + "...";

            return new Variable()
            {
                Name = value.Name,
                Presentation = presentation,
                TypeName = typeName,
                IsStructured = IsStructured(value)
            };
        }

        public IEnumerable<Variable> GetChildVariables(IValue value)
        {
            var variables = new List<IVariable>();

            if (VariableHasType(value, DataType.Object))
            {
                var objectValue = value.AsObject();
                if (HasProperties(objectValue))
                {
                    FillProperties(value, variables);
                }
                
                if(HasIndexes(objectValue as ICollectionContext))
                {
                    var context = value.AsObject();
                    if (context is IEnumerable<IValue> collection)
                    {
                        FillIndexedProperties(collection, variables);
                    }
                }
            }
            
            return variables.Select(GetVariable);
        }
        
        private void FillIndexedProperties(IEnumerable<IValue> collection, List<IVariable> variables)
        {
            int index = 0;
            foreach (var collectionItem in collection)
            {
                variables.Add(MachineVariable.Create(collectionItem, index.ToString()));
                ++index;
            }
        }

        private void FillProperties(IValue src, List<IVariable> variables)
        {
            var obj = src.AsObject();
            var propsCount = obj.GetPropCount();
            for (int i = 0; i < propsCount; i++)
            {
                var propNum = i;
                var propName = obj.GetPropName(propNum);
                
                IVariable value;

                try
                {
                    value = MachineVariable.Create(obj.GetPropValue(propNum), propName);
                }
                catch (Exception e)
                {
                    value = MachineVariable.Create(ValueFactory.Create(e.Message), propName);
                }

                variables.Add(value);
            }
        }

        private bool IsStructured(IVariable variable)
        {
            var rawValue = variable?.GetRawValue();
            return HasProperties(rawValue as IRuntimeContextInstance) 
                   || HasIndexes(rawValue as ICollectionContext);
        }

        private bool HasIndexes(ICollectionContext collection)
        {
            return collection?.Count() > 0;
        }

        private static bool HasProperties(IRuntimeContextInstance value)
        {
            return value?.GetPropCount() > 0;
        }

        private static bool VariableHasType(IValue variable, DataType type)
        {
            return variable.GetRawValue() != null && variable.DataType == type;
        }
    }
}