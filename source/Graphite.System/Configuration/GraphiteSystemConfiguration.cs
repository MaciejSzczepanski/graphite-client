﻿using System;
using System.Configuration;

namespace Graphite.System.Configuration
{
    public class GraphiteSystemConfiguration : ConfigurationSection
    {
        /// <summary>
        /// The XML name of the GraphiteSystemConfigurationSectionName Configuration Section.
        /// </summary>        
        internal const string GraphiteSystemConfigurationSectionName = "graphite.system";

        /// <summary>
        /// The XML name of the <see cref="Xmlns"/> property.
        /// </summary>        
        internal const string XmlnsPropertyName = "xmlns";

        /// <summary>
        /// The XML name of the <see cref="CounterListeners"/> property.
        /// </summary>        
        internal const string CounterListenersPropertyName = "counters";

        /// <summary>
        /// The XML name of the <see cref="EventlogListeners"/> property.
        /// </summary>        
        internal const string EventlogListenersPropertyName = "eventlog";

        /// <summary>
        /// The XML name of the <see cref="IisConfiguration"/> property.
        /// </summary>        
        internal const string ApppoolPropertyName = "appPool";

        /// <summary>
        /// The XML name of the <see cref="SystemSettingsPropertyName"/> property.
        /// </summary>        
        internal const string SystemSettingsPropertyName = "settings";

        /// <summary>
        /// Gets the W3CReadersConfiguration instance.
        /// </summary>        
        public static GraphiteSystemConfiguration Instance
        {
            get { return (GraphiteSystemConfiguration)ConfigurationManager.GetSection(GraphiteSystemConfigurationSectionName); }
        }

        /// <summary>
        /// Gets the XML namespace of this Configuration Section.
        /// </summary>
        /// <remarks>
        /// This property makes sure that if the configuration file contains the XML namespace,
        /// the parser doesn't throw an exception because it encounters the unknown "xmlns" attribute.
        /// </remarks>        
        [ConfigurationPropertyAttribute(XmlnsPropertyName)]
        public string Xmlns
        {
            get { return (string)this[XmlnsPropertyName]; }
        }

        /// <summary>
        /// Gets or sets the Listeners configuration.
        /// </summary>
        [ConfigurationPropertyAttribute(SystemSettingsPropertyName)]
        public SystemSettingsElement SystemSettings
        {
            get { return (SystemSettingsElement)this[SystemSettingsPropertyName]; }
            set { base[SystemSettingsPropertyName] = value; }
        }

        /// <summary>
        /// Gets or sets the Listeners configuration.
        /// </summary>
        [ConfigurationPropertyAttribute(CounterListenersPropertyName)]
        public CounterListenerElementCollection CounterListeners
        {
            get { return (CounterListenerElementCollection)this[CounterListenersPropertyName]; }
            set { base[CounterListenersPropertyName] = value; }
        }

        /// <summary>
        /// Gets or sets the Listeners configuration.
        /// </summary>
        [ConfigurationPropertyAttribute(EventlogListenersPropertyName)]
        public EventlogListenerElementCollection EventlogListeners
        {
            get { return (EventlogListenerElementCollection)this[EventlogListenersPropertyName]; }
            set { base[EventlogListenersPropertyName] = value; }
        }

        /// <summary>
        /// Gets or sets the App pool configuration.
        /// </summary>
        [ConfigurationPropertyAttribute(ApppoolPropertyName)]
        public AppPoolElementCollection AppPool
        {
            get { return (AppPoolElementCollection)this[ApppoolPropertyName]; }
            set { base[ApppoolPropertyName] = value; }
        }

       
    }

    public class SystemSettingsElement : ConfigurationElement
    {
        /// <summary>
        /// The XML name of the <see cref="InstanceNameCacheExpirationPropertyName"/> property.
        /// </summary>        
        internal const string InstanceNameCacheExpirationPropertyName = "instancenameCacheExpiration";


        /// <summary>
        /// The XML name of the <see cref="AppPoolListenerRefreshIntervalPropertyName"/> property.
        /// </summary>        
        internal const string AppPoolListenerRefreshIntervalPropertyName = "appPoolListenerRefreshInterval";

        /// <summary>
        /// Gets or sets the InstanceNameCacheExpiration configuration.
        /// </summary>
        [ConfigurationPropertyAttribute(InstanceNameCacheExpirationPropertyName)]
        public int InstanceNameCacheExpiration
        {
            get { return Convert.ToInt32(this[InstanceNameCacheExpirationPropertyName]); }
            set { base[InstanceNameCacheExpirationPropertyName] = value; }
        }
        
        /// <summary>
        /// Gets or sets the InstanceNameCacheExpiration configuration.
        /// </summary>
        [ConfigurationPropertyAttribute(AppPoolListenerRefreshIntervalPropertyName)]
        public int AppPoolListenerRefreshInterval
        {
            get { return Convert.ToInt32(this[AppPoolListenerRefreshIntervalPropertyName]); }
            set { base[AppPoolListenerRefreshIntervalPropertyName] = value; }
        }
    }
}
