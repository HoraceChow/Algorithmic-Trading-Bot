/*
This is an algorithm determines market trends channels and plot them on the chart. Which shows the current market trend, and potentially future market trend, and potential price bouncing off the trend channel.
This algorithm can be used for optimizing a trading bot, by assisting the strategy to make trading decision that is favouring the market trend.
Trading with the market trend would increase probability of successful trades.
*/

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Strategies{
	public class AutoTrendChannel : Strategy{
		private int lastHighBar = -1;
		private int lastLowBar = -1;
		private double lastHighPrice = double.MinValue;
		private double lastLowPrice = double.MaxValue;
		private bool trendBreak = true;
		private TrendChannels CurrentTrend;
		private TrendQueue trendChannel;
		private Swing swing;

		protected override void OnStateChange(){	//Runs when strategy is initialized
			if (State == State.SetDefaults){
				Description	= @"Algorithm that automatically plots trend channel";
				Name = "AutoTrendChannel";
				Calculate = Calculate.OnBarClose;
				Strength = 5;
				NumberOfTrendChannel = 5;
				EntriesPerDirection	= 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				IsFillLimitOnTouch = false;
				MaximumBarsLookBack	= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution	= OrderFillResolution.Standard;
				Slippage = 0;
				StartBehavior = StartBehavior.WaitUntilFlat;
				TimeInForce	= TimeInForce.Gtc;
				TraceOrders	= false;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade	= 4;
				IsInstantiatedOnEachOptimizationIteration = true;
			}
			else if (State == State.Configure){		//Runes when strategy is added onto the chart
				AddDataSeries("ES 09-20", Data.BarsPeriodType.Tick, 2000);
			}
			else if (State == State.DataLoaded){	//Called after data has been loaded
				swing = Swing(Input, Strength);
				trendChannel = new TrendQueue(this, NumberOfTrendChannel);
			}
		}

		protected override void OnBarUpdate(){ //Runs each time a new bar is formed
			if (BarsInProgress == 0){ //Runs everytime a new 2000 Ticks Bar is formed
				if (CurrentBar < 0 || CurrentBar==null){return;}

				//Last swing high is higher than the most recent swing high, means market is trending down
				int swingHighBar = swing.SwingHighBar(0, 1, Strength + 1);
				if (swingHighBar != -1){ //swing point is found within the lookBackPeriod
					double swingHighPrice = High[swingHighBar];
					if (swingHighPrice < lastHighPrice && lastHighBar > -1){
						if(trendBreak){
							CurrentTrend = new TrendChannels(lastHighBar, lastHighPrice, CurrentBar - swingHighBar, swingHighPrice, lastLowBar, lastLowPrice) { IsHigh = false };
							trendChannel.Enqueue(CurrentTrend);
							trendBreak = false;
						}
					}
					lastHighBar	= CurrentBar - swingHighBar;
					lastHighPrice = swingHighPrice;
				}

				//Last swing low is lower than the most recent swing low, means market is trending up
				int swingLowBar = swing.SwingLowBar(0, 1, Strength + 1);
				if (swingLowBar != -1){ //swing point is found within the lookBackPeriod
					double swingLowPrice = Low[swingLowBar];
					if (swingLowPrice > lastLowPrice && lastLowBar > -1){
						if(trendBreak){
							CurrentTrend = new TrendChannels(lastLowBar, lastLowPrice, CurrentBar - swingLowBar, swingLowPrice, lastHighBar, lastHighPrice){ IsHigh = true };
							trendChannel.Enqueue(CurrentTrend);
							trendBreak = false;
						}
					}
					lastLowBar	= CurrentBar - swingLowBar;
					lastLowPrice = swingLowPrice;
				}

				if(!trendBreak){
					//top and bottom of the trend channel
					double highRay = CurrentTrend.ParallelPrice+(CurrentBar-CurrentTrend.ParallelBar)*CurrentTrend.Slope;
					double lowRay = CurrentTrend.EndPrice+(CurrentBar-CurrentTrend.EndBar)*CurrentTrend.Slope;

					//if price breaks the trend channel, new trend channel is forming
					if(Close[0]>highRay+0.5*TickSize||Close[0]<lowRay-0.5*TickSize){ //0.5*TickSize for filtering false breakout
						Draw.Text(this, "broken"+CurrentBar, "broken", 0, High[0], Brushes.Black);
						trendBreak = true;
						CurrentTrend = null; 
					}
				}
			} 
			else{return;}
		}
		#region Helpers
		private class TrendChannels{
			public int StartBar;
			public double StartPrice;
			public int EndBar;
			public double EndPrice;
			public int ParallelBar;
			public double ParallelPrice;
			public TrendChannel	Trend;
			public bool IsHigh;
			public double Slope;
			public TrendChannels(int startBar, double startPrice, int endBar, double endPrice, int parallelBar, double parallelPrice){
				StartBar = startBar;
				StartPrice = startPrice;
				EndBar = endBar;
				EndPrice = endPrice;
				ParallelBar = parallelBar;
				ParallelPrice = parallelPrice;
				Slope = (EndPrice-StartPrice)/(EndBar-StartBar);
			}
		}
		private class TrendQueue : Queue<TrendChannels>{
			private AutoTrendChannel instance;
			private TrendChannels lastTrend;

			public new void Enqueue(TrendChannels trend){
				if (instance.ChartControl != null){
					string channelName	= string.Format("{0}_{1}", (trend.IsHigh)? "TrendChannelTrendHigh" : "TrendChannelTrendLow", trend.StartBar);
					trend.Trend	= Draw.TrendChannel(instance,
													channelName,
													true,
													instance.CurrentBar - trend.StartBar,
													trend.StartPrice,
													instance.CurrentBar - trend.EndBar,
													trend.EndPrice,
													instance.CurrentBar - trend.ParallelBar,
													trend.ParallelPrice);
				}
				lastTrend = trend;
				base.Enqueue(trend);

				if (Count > instance.NumberOfTrendChannel){
					TrendChannels toRemove = base.Dequeue();
					if (toRemove.Trend != null)
						instance.RemoveDrawObject(toRemove.Trend.Tag);
				}
			}
			public TrendQueue(AutoTrendChannel instance, int capacity) : base(capacity){
				this.instance = instance;
			}
		}
		#endregion

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Strength", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Strength { get; set; }

		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NumberOfTrendLines", GroupName = "NinjaScriptParameters", Order = 1)]
		public int NumberOfTrendChannel { get; set; }
	}
}
