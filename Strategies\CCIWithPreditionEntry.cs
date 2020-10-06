/*
This trading bot uses the CCI cross strategy, where whenever
	- CCI Value is below 100 on previous price bar and CCI Value is Above 100 on current price bar, then will enter long 
	- CCI Value is below -100 on previous price bar and CCI Value is Above -100 on current price bar, then will enter long
	- CCI Value is above 100 on previous price bar and CCI Value is below 100 on current price bar, then will enter short 
	- CCI Value is above -100 on previous price bar and CCI Value is below -100 on current price bar, then will enter short
However, the limitation of CCI cross strategy is that once the current price bar has formed, the CCI value could possibility be far off from CCI=100 or CCI=-100.
This trading bot is able to enter while the price bar is still forming, by calculating the estimate price of CCI.
Which secures more profits, and entering trades before the current price bar is formed allow the best entry in case of price exhaustion 
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
		private bool sellEntryBar = false;
		private bool buyEntryBar = false;
		private double lastHigh;
		private double lastClose;
		private double lastLow;
		private int Stoplossstorer = 1;
		
		private Swing swing;
		private int isUp = 0;
		private int lastCurrentBar = 0;
		private int lastHighBar = -1;
		private int lastLowBar = -1;
		private double lastHighPrice = double.MinValue;
		private double lastLowPrice = double.MaxValue;

		protected override void OnStateChange(){	//Runs when strategy is initialized
			if (State == State.SetDefaults){
				Description	= @"Trading using an estimation value of the CCI indictor, which secure more profits and entering trades before the current price bar formed allow the best entry";
				Name = "CCIWithPreditionEntry";
				Calculate = Calculate.OnBarClose;
				EntriesPerDirection	= 1;
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
			else if (State == State.Configure){		//Runes when strategy is added onto the chart
				AddDataSeries(Data.BarsPeriodType.Tick, 1);	
				AddDataSeries("ES 09-20", Data.BarsPeriodType.Tick, 2000);	//Using 2000 tick charts -> each price bar
				SetStopLoss(CalculationMode.Ticks, 12);
			}
			else if (State == State.DataLoaded){	//Called after data has been loaded
				AddChartIndicator(CCI(14));
				swing = Swing(Input, 2);
			}
		}

		protected override void OnBarUpdate(){ //Runs each time a new bar is formed
			if (BarsInProgress == 0){ //Runs everytime a new 2000 Ticks Bar is formed
				if(CurrentBar == null || CurrentBar < BarsRequiredToTrade){return;}
                sellEntryBar = false;
                buyEntryBar = false;

				//Last swing high is higher than the most recent swing high, which means market is trending down
				int swingHighBar = swing.SwingHighBar(0, 1, 2 + 1);
				if (swingHighBar == 2){ 
					double swingHighPrice = High[swingHighBar];
					if (swingHighPrice < lastHighPrice && isUp==0 && lastHighBar > -1){isUp = -1;}
					else if(swingHighPrice > lastHighPrice && isUp==-1 && lastHighBar > -1){isUp=0;}
					lastHighBar	= CurrentBar - swingHighBar;
					lastHighPrice = swingHighPrice;
				}
				//Last swing low is lower than the most recent swing low, which means market is trending up
				int swingLowBar = swing.SwingLowBar(0, 1, 2 + 1);
				if (swingLowBar == 2){
					double swingLowPrice = Low[swingLowBar];
					if (swingLowPrice > lastLowPrice && isUp==0 && lastLowBar > -1){isUp = 1;}
					else if (swingLowPrice < lastLowPrice && isUp==1 && lastLowBar > -1){isUp=0;}
					lastLowBar	= CurrentBar - swingLowBar;
					lastLowPrice = swingLowPrice;
				}
				if(Close[0]>lastHighPrice && isUp == -1){isUp = 1; }
				if(Close[0]<lastLowPrice && isUp == 1){isUp = -1;}
			}

			else if(BarsInProgress == 1){ //Runs every single tick
				if(CurrentBars[0]==null || CurrentBars[0]<BarsRequiredToTrade){return;} //doesn't run if no bars has formed yet

				if(lastCurrentBar != CurrentBars[0]){
					lastHigh = High[0];
					lastLow = Low[0];
				}
				lastClose = Close[0];
				if(High[0]>lastHigh){lastHigh = High[0];}
				if(Low[0]<lastLow){lastLow = Low[0];}
				lastCurrentBar = CurrentBars[0];

				if(Position.MarketPosition == MarketPosition.Flat){ //runs if currently, there are no open positions 
					SetStopLoss(CalculationMode.Ticks, 12);
					Stoplossstorer = 1;
					if(ToTime(Time[0]) >= 73000 && ToTime(Time[0]) < 153000){	//allow entering trades during market open
						if(isUp==1 &&(CCI(Closes[0],14)[0]<=100 && CCIPredictor(this,14)>=100 || CCI(Closes[0],14)[0]<=-100 && CCIPredictor(this,14)>=-100)){EnterLong(1);}
						if(isUp==-1 &&(CCI(Closes[0],14)[0]>=-100 && CCIPredictor(this,14)<=-100 || CCI(Closes[0],14)[0]>=100 && CCIPredictor(this,14)<=100)){EnterShort(1);}
					}
				}
				//trailing stoploss for collecting profit and securing gains
				else if (Position.MarketPosition == MarketPosition.Long){
					while(Close[0] >= Position.AveragePrice + Stoplossstorer*2){ 
                        SetStopLoss(CalculationMode.Price, Position.AveragePrice+2*(Stoplossstorer-1)); 
                        Stoplossstorer++;
                    }
                }
                else if (Position.MarketPosition == MarketPosition.Short){
                    while(Close[0] <= Position.AveragePrice - Stoplossstorer*2){
                        SetStopLoss(CalculationMode.Price, Position.AveragePrice-2*(Stoplossstorer-1));
                        Stoplossstorer++;
                    }                        
                }
			}

			else{return;}
		}

		#region Helpers 
		public double CCIPredictor(mypointstrate instance, int period){	//Calculating predicted CCI value
			if(instance.CurrentBars[0] <= 0){return 0;}			
			else{
				double mean = 0;
				double last = SMA(instance.Typicals[0],period)[0]*Math.Min(instance.CurrentBars[0]+1,period);
				double estimatedSMA;

				if(instance.CurrentBars[0]+1>=period){estimatedSMA = (last + (lastHigh+lastClose+lastLow)/3 - instance.Typicals[0][period-1])/Math.Min(instance.CurrentBars[0]+1,period);}
				else{estimatedSMA = (last + (lastHigh+lastClose+lastLow)/3)/(Math.Min(instance.CurrentBars[0]+1,period)+1);}
				for(int index = Math.Min(instance.CurrentBars[0],period-2); index>=0; index--){mean += Math.Abs(instance.Typicals[0][index] - estimatedSMA);}
				mean += Math.Abs((lastHigh+lastClose+lastLow)/3 - estimatedSMA);
				return ((lastHigh+lastClose+lastLow)/3 - estimatedSMA)/ (mean.ApproxCompare(0) == 0 ? 1 : (0.015 * (mean / Math.Min(period, CurrentBar + 1))));
			}
		}
		#endregion
	}
}


