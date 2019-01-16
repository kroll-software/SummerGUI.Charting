using System;

// stop namespace pollution by hiding these contents from the main namespace
namespace SummerGUI.Charting.PerfCharts
{
	/// <summary>
	/// Scale mode for value aspect ratio
	/// </summary>
	public enum ScaleModes
	{
		/// <summary>
		/// Absolute Scale Mode: Values from 0 to 100 are accepted and displayed
		/// </summary>
		Absolute,
		/// <summary>
		/// Relative Scale Mode: All values are allowed and displayed in a proper relation
		/// </summary>
		Relative
	}


	/// <summary>
	/// Chart Refresh Mode Timer Control Mode
	/// </summary>
	public enum TimerModes
	{
		/// <summary>
		/// Chart is refreshed when a value is added
		/// </summary>
		Disabled,
		/// <summary>
		/// Chart is refreshed every <c>TimerInterval</c> milliseconds, adding all values
		/// in the queue to the chart. If there are no values in the queue, a 0 (zero) is added
		/// </summary>
		Simple,
		/// <summary>
		/// Chart is refreshed every <c>TimerInterval</c> milliseconds, adding an average of
		/// all values in the queue to the chart. If there are no values in the queue,
		/// 0 (zero) is added
		/// </summary>
		SynchronizedAverage,
		/// <summary>
		/// Chart is refreshed every <c>TimerInterval</c> milliseconds, adding the sum of
		/// all values in the queue to the chart. If there are no values in the queue,
		/// 0 (zero) is added
		/// </summary>
		SynchronizedSum
	}
}

