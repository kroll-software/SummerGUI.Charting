using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace SummerGUI.Charting.Graph2D
{
    [Serializable]
    [TypeConverter(typeof(ExpandableObjectConverter))]    
    public class GraphList : Dictionary<string, GraphBase>
    {
        public GraphList()            
        {
        }

		public void Add(GraphBase graph)
		{
			if (graph == null)
				return;

			if (String.IsNullOrEmpty (graph.Key))
				graph.Key = System.Guid.NewGuid ().ToString ();

			if (this.ContainsKey (graph.Key))
				throw new ArgumentException ("Key exists");

			base.Add (graph.Key, graph);
		}

        public void FinalizeSerialization()
        {
            foreach (GraphBase g in this.Values)
                g.FinalizeDeserialization(this);
        }
    }
}
