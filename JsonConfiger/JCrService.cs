﻿using JsonConfiger.Models;
using JsonConfiger.WPF;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace JsonConfiger
{
    public class JCrService
    {
        private (ObservableCollection<CNode> Nodes, ObservableCollection<CProperty> Properties) ResolveJson(JObject data, JObject descObj)
        {
            var childNodes = new ObservableCollection<CNode>();
            var properties = new ObservableCollection<CProperty>();
            dynamic descInfo = null;
            //var descList = descObj as ICollection<KeyValuePair<string, JToken>>;
            if (data != null)
                foreach (var x in data)
                {
                    if (descObj != null)
                        descInfo = descObj[x.Key];
                    if (x.Value is JValue)
                    {
                        var value = x.Value as JValue;
                        CProperty property = ConverterToNodeProperty(value);
                        if (descInfo != null)
                        {
                            bool ok = Enum.TryParse(descInfo.type.ToString(), true, out CPropertyType cType);
                            if (ok)
                                property.CType = cType;

                            property.Lan = descInfo.lan;
                        }

                        property.Name = x.Key;

                        if (property != null)
                            properties.Add(property);
                    }
                    else
                    {
                        var node = new CNode();
                        if (descInfo != null)
                            node.Lan = descInfo.lan;

                        node.Name = x.Key;

                        var r = ResolveJson(x.Value as JObject, descInfo as JObject);
                        node.Children = r.Nodes;
                        node.Properties = r.Properties;
                        childNodes.Add(node);
                    }
                }

            return (childNodes, properties);
        }

        private CProperty ConverterToNodeProperty(JValue value)
        {
            if (value == null)
                return null;

            CProperty result = new CProperty();
            result.Value = value.Value;
            bool ok = Enum.TryParse(value.Type.ToString(), out CPropertyType Type);
            if (!ok)
                return null;

            result.CType = Type;
            return result;
        }

        public JsonConfierViewModel GetVM(object config, object descConfig)
        {
            var json = config as JObject;
            if (json == null)
                return null;

            var vm = new JsonConfierViewModel();
            vm.Nodes = ResolveJson(config as JObject, descConfig as JObject).Nodes;
            return vm;
        }

        public UserControl GetView(object config, object desc)
        {
            var control = new JsonConfierControl();
            control.DataContext = GetVM(config, desc);
            return control;
        }

        public Object GetData(ObservableCollection<CNode> nodes)
        {
            var result = new ExpandoObject() as IDictionary<string, Object>;
            foreach (var nodeItem in nodes)
            {
                var tempNodeObj = GetDataFromNode(nodeItem);

                foreach (var subNode in nodeItem.Children)
                {
                    var subNodeObj = GetDataFromNode(subNode);
                    tempNodeObj.Add(subNode.Name, subNodeObj);
                }
                result.Add(nodeItem.Name, tempNodeObj);
            }
            return result;
        }

        private IDictionary<string, Object> GetDataFromNode(CNode nodeItem)
        {
            var tempNodeObj = new ExpandoObject() as IDictionary<string, Object>;
            foreach (var propertyItem in nodeItem.Properties)
            {
                tempNodeObj.Add(propertyItem.Name, propertyItem.Value);
            }
            return tempNodeObj;
        }
    }
}
