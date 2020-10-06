/*
This trading bot uses the two legged pull back pattern.
This trading bot is implemented with the AutoTrendChannel algorithm (included within repository), which allow the trading bot to 
determining market direction, and trading with the trend increasing probability of winning trades.
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
	public class mypointstrate : Strategy{
		private buy_2lpb buy_leg = null;
		private sell_2lpb sell_leg = null;
		private bool sellEntryBar = false;
		private bool buyEntryBar = false;
		private static bool enter_buy = false;
		private static bool enter_sell = false;

		private int lastHighBar	= -1;
		private int lastLowBar = -1;
		private double lastHighPrice = double.MinValue;
		private double lastLowPrice = double.MaxValue;
		private bool trendBreak = true;
		private TrendChannels CurrentTrend;
		private TrendQueue trendChannel;
		private Swing swing;

		protected override void OnStateChange(){	//Runs when strategy is initialized
			if (State == State.SetDefaults){
				Description	= @"Two Leg pull back pattern with AutoTrendChannel";
				Name = "mypointstrate";
				Calculate = Calculate.OnBarClose;
				Strength = 5;
				NumberOfTrendChannel = 5;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				IsFillLimitOnTouch = false;
				MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution = OrderFillResolution.Standard;
				Slippage = 0;
				StartBehavior = StartBehavior.WaitUntilFlat;
				TimeInForce = TimeInForce.Gtc;
				TraceOrders	= false;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade = 5;
				IsInstantiatedOnEachOptimizationIteration = true;
			}
			else if (State == State.Configure){ 	//Runes when strategy is added onto the chart
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
				AddDataSeries("ES 09-20", Data.BarsPeriodType.Tick, 2000); //Using 2000 tick charts -> each price bar
				SetProfitTarget(CalculationMode.Ticks, 4);
				SetStopLoss(CalculationMode.Ticks, 8);
			}
			else if (State == State.DataLoaded){	//Called after data has been loaded
				swing = Swing(Input, Strength);
				trendChannel = new TrendQueue(this, NumberOfTrendChannel);
			}
		}

		protected override void OnBarUpdate(){ //anytime a bar closes, so every 2000 ticks, this method get called
			if (BarsInProgress == 0){ //Runs everytime a new 2000 Ticks Bar is formed
				if(CurrentBar == null || CurrentBar < BarsRequiredToTrade){return;}

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

				if(buy_leg==null){ 
					buy_leg = new buy_2lpb(this,High[0]);
					sell_leg = new sell_2lpb(this,Low[0]);
				}
				buy_leg.Setup(ref buy_leg);
				sell_leg.Setup(ref sell_leg);
				sellEntryBar = false;
				buyEntryBar = false;
				if(enter_buy){IntraBar(this, true);}
				if(enter_sell){IntraBar(this, false);}
			}
			else if (BarsInProgress == 1){ //Runs within each 5 tick bar
				if(ToTime(Time[0]) >= 73000 && ToTime(Time[0]) < 153000){	//allow entering trades during market open
					if(enter_buy&&(Close[0]>=(buy_leg.legs[3]+0.25))&&buyEntryBar&&CurrentTrend.IsHigh){
						enter_buy=false;
						EnterLong(1, "My Buy Entry");
					}	
					if(enter_sell&&(Close[0]<=(sell_leg.legs[3]-0.25))&&sellEntryBar&&CurrentTrend.IsHigh){
						enter_sell=false;
						EnterShort(1, "My Sell Entry");
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
			public TrendChannel Trend;
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
			private mypointstrate instance;
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
			public TrendQueue(mypointstrate instance, int capacity) : base(capacity){
				this.instance = instance;
			}
		}
		public void IntraBar(mypointstrate instance, bool isBuy){	//Determining is market on your favour based on previous bar
			if(High[1]>High[0]&&Low[1]<Low[0]){return;}
			if(isBuy){
				if(instance.Close[0]-instance.Open[0]>0){
					double barTop = instance.High[0]-instance.Close[0];
					double barHeight = instance.Close[0] - instance.Open[0];
					double barBottom = instance.Open[0] - instance.Low[0];
					if(barTop<=barHeight && barBottom>=(barTop+barHeight) && barBottom>=0.5
						||barTop*2<=barHeight && barBottom*2<=barHeight && barHeight>=0.5){buyEntryBar = true;}
				}
				else if(instance.Close[0]-instance.Open[0]<0){
					double barTop = instance.High[0]-instance.Open[0];
					double barHeight = instance.Open[0] - instance.Close[0];
					double barBottom = instance.Close[0] - instance.Low[0];
					if(barBottom>=2*barHeight && barTop<=barHeight+0.25 && barHeight<=0.5){buyEntryBar = true;}
				}
				else{
					double barTop = instance.High[0]-instance.Open[0];
					double barBottom = instance.Open[0] - instance.Low[0];
					if(barBottom>2*barTop && barBottom>=0.75){buyEntryBar = true;}
				}
			}
			else{
				if(instance.Close[0]-instance.Open[0]<0){
					double barTop = instance.High[0]-instance.Open[0];
					double barHeight = instance.Open[0] - instance.Close[0];
					double barBottom = instance.Close[0] - instance.Low[0];
					if(barBottom<=barHeight && barTop>=(barBottom+barHeight) && barTop>=0.5
						||barBottom*2<=barHeight && barTop*2<=barHeight && barHeight>=0.5){sellEntryBar = true;}
				}
				else if(instance.Close[0]-instance.Open[0]>0){
					double barTop = instance.High[0]-instance.Close[0];
					double barHeight = instance.Close[0] - instance.Open[0];
					double barBottom = instance.Open[0] - instance.Low[0];
					if(barTop>=2*barHeight && barBottom<=barHeight+0.25 && barHeight<=0.5){sellEntryBar = true;}
				}
				else{
					double barTop = instance.High[0]-instance.Open[0];
					double barBottom = instance.Open[0] - instance.Low[0];
					if(barTop>2*barBottom && barTop>=0.75){sellEntryBar = true;}
				}
			}
		}
		
		//Pattern Setup
		public class buy_2lpb{
			mypointstrate Instance;
			public double[] legs;
			public double reference;
			public buy_2lpb(mypointstrate instance, double pull_back){
				Instance = instance;
				legs = new double[7];
				legs[0] = pull_back;
				reference = pull_back;
			} 
			public void Setup(ref buy_2lpb my_setup){
				double High = Instance.High[0];
				if(legs[1]==0){
					if(High>reference){reference = High; legs[0] = High;}
					else if(High<reference){reference = High; legs[1] = High;}
				}
				else if(legs[2]==0){
					if(High<reference){reference = High; legs[1] = High;}
					else if(High>legs[0]){my_setup = new buy_2lpb(Instance, High);}
					else if(High>reference){reference = High; legs[2] = High;}
				}
				else if(legs[3]==0){
					if(High>legs[0]){my_setup = new buy_2lpb(Instance, High);}
					else if(High>reference){reference = High; legs[2] = High;}
					else if(High<reference){reference = High; legs[3] = High; enter_buy = true;}
				}
				else if(legs[4]==0){
					if(High<reference){reference = High; legs[3] = High; enter_buy = true;}
					else if(High>legs[2]){my_setup = new buy_2lpb(Instance, High); enter_buy = false;}
					else if(High>reference){reference = High; legs[4] = High; enter_buy = false;}
				}
				else if(legs[5]==0){
					if(High>legs[2]){my_setup = new buy_2lpb(Instance, High);}
					else if(High>reference){reference = High; legs[4] = High;}
					else if(High<reference){reference = High; legs[5] = High;}
				}
				else if(legs[6]==0){
					if(High<reference){reference = High; legs[5] = High;}
					else if(High>legs[4]){my_setup = new buy_2lpb(Instance, High);}
					else if(High>reference){reference = High; legs[6] = High;}
				}
				else{
					if(High>legs[4]){my_setup = new buy_2lpb(Instance, High);}
					else if(High>reference){reference = High; legs[6] = High;}
					else if(High<reference){reference = High; legs[4] = legs[6]; legs[5] = High; legs[6] = 0;}
				}
			}
		}
		public class sell_2lpb{
			mypointstrate Instance;
			public double[] legs;
			public double reference;
			public sell_2lpb(mypointstrate instance, double pull_back){
				Instance = instance; 
				legs = new double[7];
				legs[0] = pull_back;
				reference = pull_back;
			}
			public void Setup(ref sell_2lpb my_setup){
				double Low = Instance.Low[0];
				if(legs[1]==0){
					if(Low<reference){reference = Low; legs[0] = Low;}
					else if(Low>reference){reference = Low; legs[1] = Low;}
				}
				else if(legs[2]==0){
					if(Low>reference){reference = Low; legs[1] = Low;}
					else if(Low<legs[0]){my_setup = new sell_2lpb(Instance, Low);}
					else if(Low<reference){reference = Low; legs[2] = Low;}
				}
				else if(legs[3]==0){
					if(Low<legs[0]){my_setup = new sell_2lpb(Instance, Low);}
					else if(Low<reference){reference = Low; legs[2] = Low;}
					else if(Low>reference){reference = Low; legs[3] = Low;}
				}
				else if(legs[4]==0){
					if(Low>reference){reference = Low; legs[3] = Low; enter_sell = true;}
					else if(Low<legs[2]){my_setup = new sell_2lpb(Instance, Low); enter_sell = false;}
					else if(Low<reference){reference = Low; legs[4] = Low; enter_sell = false;}
				}
				else if(legs[5]==0){
					if(Low<legs[2]){my_setup = new sell_2lpb(Instance, Low);}
					else if(Low<reference){reference = Low; legs[4] = Low;}
					else if(Low>reference){reference = Low; legs[5] = Low;}
				}
				else if(legs[6]==0){
					if(Low>reference){reference = Low; legs[5] = Low;}
					else if(Low<legs[4]){my_setup = new sell_2lpb(Instance, Low);}
					else if(Low<reference){reference = Low; legs[6] = Low;}
				}
				else{
					if(Low<legs[4]){my_setup = new sell_2lpb(Instance, Low);}
					else if(Low<reference){reference = Low; legs[6] = Low;}
					else if(Low>reference){reference = Low; legs[4] = legs[6]; legs[5] = Low;legs[6] = 0;}
				}
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

