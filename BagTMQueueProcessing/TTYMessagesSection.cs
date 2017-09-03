using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;

namespace BagTMQueueProcessing
{
    
    public class TTYMessagesSection : ConfigurationSection
    {
         [ConfigurationProperty("", IsDefaultCollection = true)]  
         public TTYCollection ttyCollection
         {
              get
              {
                  TTYCollection ttyCollection = (TTYCollection)base[""];
 
                  return ttyCollection;                
 
              }
         }
    }

    public class TTYCollection : ConfigurationElementCollection
    {
        public TTYCollection()
        {
            TTYConfigElement details = (TTYConfigElement)CreateNewElement();
            if (details.key != "")
            {
                Add(details);
            }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }
 
        protected override ConfigurationElement CreateNewElement()
        {
            return new TTYConfigElement();
        }
 
        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((TTYConfigElement)element).key;
        }
 
        public TTYConfigElement this[int index]
        {
            get
            {
                return (TTYConfigElement)BaseGet(index);
            }

            set
            {
                if (BaseGet(index) != null)
                {
                   BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }
   
        new public TTYConfigElement this[string key]
        {
            get
            {
                return (TTYConfigElement)BaseGet(key);
            }
        }
 
        public int IndexOf(TTYConfigElement details)
        {
            return BaseIndexOf(details);
        }
   
        public void Add(TTYConfigElement details)
        {
            BaseAdd(details);
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);
        }
   
        public void Remove(TTYConfigElement details)
        {
            if (BaseIndexOf(details) >= 0)
                 BaseRemove(details.key);
        }
   
        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }
  
        public void Remove(string key)
        {
            BaseRemove(key);
        }
  
        public void Clear()
        {
            BaseClear();
        }
  
        protected override string ElementName
        {
            get { return "tty"; }
        }

        public int GetNumberOfItems ()
        {
            return BaseGetAllKeys().Length;
        }

        public bool HasTTY(String key)
        {
            return ((TTYConfigElement)BaseGet(key) == null) ? false : true ;
        }

        public String TTYType(String key)
        {
            return ((TTYConfigElement)BaseGet(key)).typeTTY;
        }

        public bool HasTTYElement(String TTYKey, String TTYElementKey)
        {
            TTYConfigElement config = (TTYConfigElement)BaseGet(TTYKey);

            return (config != null && config.ttyElements.HasTTYElement(TTYElementKey)) ? true : false;
        }

        public String Describe ()
        {
            String response = "";

            TTYConfigElement config;

            for (int i = 0; i < base.Count; i++)
            {
                config = (TTYConfigElement)BaseGet(i);
                
                for (int j = 0; j < config.ttyElements.Count; j++)
                {
                    response.Insert(0, config.ttyElements[j].key);
                    response.Insert(0, "");
                }

                response.Insert(0, config.key);
            }

            return response;
        }

        public TTYConfigElement GetTTYElement(string key)
        {
            return (TTYConfigElement)BaseGet(key);
        }

        public new IEnumerator<TTYConfigElement> GetEnumerator()
        {
            foreach (var key in this.BaseGetAllKeys())
            {
                yield return (TTYConfigElement)BaseGet(key);
            }
        }
    }

    public class TTYConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("key", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/;’\"|\\")]
        public string key
        {
            get { return (string)this["key"]; }
            set { this["key"] = value; }
        }

        [ConfigurationProperty("value", IsRequired = true, IsKey = false)]
        [StringValidator(InvalidCharacters = "^[0-9]")]
        public string value
        {
            get { return (string)this["value"]; }
            set { this["value"] = value; }
        }

        [ConfigurationProperty("typeTTY", IsRequired = false, IsKey = false)]
        [StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/;’\"|\\")]
        public string typeTTY
        {
            get { return (string)this["typeTTY"]; }
            set { this["typeTTY"] = value; }
        }

        [ConfigurationProperty("entityName", IsRequired = true, IsKey = false)]
        [StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/;’\"|\\")]
        public string entityName
        {
            get { return (string)this["entityName"]; }
            set { this["entityName"] = value; }
        }

        [ConfigurationProperty("ttyelements", IsDefaultCollection = false)]
        public TTYElementsCollection ttyElements
        {
            get { return (TTYElementsCollection)base["ttyelements"]; }
        }

    }

    public class TTYElementsCollection : ConfigurationElementCollection
    {

        public new TTYElementConfigElement this[string key]
        {
            get
            {
                if (IndexOf(key) < 0) return null;

                return (TTYElementConfigElement)BaseGet(key);
            }
        }

        public TTYElementConfigElement this[int index]
        {
            get { return (TTYElementConfigElement)BaseGet(index); }
        }

        public int IndexOf(string key)
        {
            key = key.ToLower();

            for (int idx = 0; idx < base.Count; idx++)
            {
                if (this[idx].key.ToLower() == key)
                    return idx;
            }
            return -1;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new TTYElementConfigElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TTYElementConfigElement)element).key;
        }

        protected override string ElementName
        {
            get { return "ttyelement"; }
        }

        public TTYElementConfigElement GetTTYElementElement(string key)
        {
            return (TTYElementConfigElement)BaseGet(key);
        }

        public bool HasTTYElement(String key)
        {
            return ((TTYElementConfigElement)BaseGet(key) == null) ? false : true;
        }
    }

    public class TTYElementConfigElement : ConfigurationElement
    {

        public TTYElementConfigElement()
        {
        }

        [ConfigurationProperty("key", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = "[A-Z]")]
        public string key
        {
            get { return (string)this["key"]; }
            set { this["key"] = value; }
        }

        [ConfigurationProperty("value", IsRequired = false, IsKey = false)]
        [StringValidator(InvalidCharacters = "^[0-9]")]
        public string value
        {
            get { return (string)this["value"]; }
            set { this["value"] = value; }
        }

        [ConfigurationProperty("ttycomponents", IsDefaultCollection = false)]
        public TTYComponentsCollection ttycomponents
        {
            get { return (TTYComponentsCollection)base["ttycomponents"]; }
        }
    }

    public class TTYComponentsCollection : ConfigurationElementCollection, IEnumerable<TTYComponentConfigElement>

    {

        public new TTYComponentConfigElement this[string methodName]
        {
            get
            {
                if (IndexOf(methodName) < 0) return null;

                return (TTYComponentConfigElement)BaseGet(methodName);
            }
        }

        public TTYComponentConfigElement this[int index]
        {
            get { return (TTYComponentConfigElement)BaseGet(index); }
        }

        public int IndexOf(string methodName)
        {
            methodName = methodName.ToLower();

            for (int idx = 0; idx < base.Count; idx++)
            {
                if (this[idx].methodName.ToLower() == methodName)
                    return idx;
            }
            return -1;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new TTYComponentConfigElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TTYComponentConfigElement)element).methodName;
        }

        protected override string ElementName
        {
            get { return "ttycomponent"; }
        }

        public TTYComponentConfigElement GetTTYElementElement(string methodName)
        {
            return (TTYComponentConfigElement)BaseGet(methodName);
        }

        public new IEnumerator<TTYComponentConfigElement> GetEnumerator()
        {
            foreach (var key in this.BaseGetAllKeys())
            {
                yield return (TTYComponentConfigElement)BaseGet(key);
            }
        }

    }

    public class TTYComponentConfigElement : ConfigurationElement
    {
        /// <summary>
        /// Substring xml attribute divider
        /// </summary>
        private String substringSplit = @";";

        public TTYComponentConfigElement()
        {
        }

        [ConfigurationProperty("position", IsRequired = true, IsKey = false)]
        [StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/;’\"|\\")]
        public string position
        {
            get { return (string)this["position"]; }
            set { this["position"] = value; }
        }

        [ConfigurationProperty("substring", IsRequired = false, IsKey = false)]
        [StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/.’\"|\\")]
        public string substring
        {
            get { return (string)this["substring"]; }
            set { this["substring"] = value; }
        }

        [ConfigurationProperty("methodName", IsRequired = true, IsKey = true)]
        [StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/;’\"|\\")]
        public string methodName
        {
            get { return (string)this["methodName"]; }
            set { this["methodName"] = value; }
        }

        [ConfigurationProperty("repetition", IsRequired = false, IsKey = true)]
        [StringValidator(InvalidCharacters = "  ~!@#$%^&*()[]{}/;’\"|\\")]
        public string repetition
        {
            get { return (string)this["repetition"]; }
            set { this["repetition"] = value; }
        }

        public int StartSubString()
        {
            String[] substring = Regex.Split(this.substring, this.substringSplit);
            return (substring != null && substring.Length > 0) ? Convert.ToInt32(substring[0]) : 0;
        }

        public int LenghtSubString()
        {
            String[] substring = Regex.Split(this.substring, this.substringSplit);
            return (substring != null && substring.Length > 1) ? Convert.ToInt32(substring[1]) : 0;
        }

        public bool IsRepetition()
        {
            return (repetition != null && repetition.ToUpper().Equals("TRUE"));
        }

        /// <summary>
        /// Transformation to int and removing 1 to be compatible with C# arrays
        /// </summary>
        /// <returns></returns>
        public int GetPositionInt()
        {
            return Convert.ToInt32(this.position)-1;
        }
    }
}
