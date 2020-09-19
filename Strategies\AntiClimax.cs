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

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies{
	public class mypointstrate : Strategy{
		
		private bool LongSetup = false;
		private double LongLimitLine;
		private bool ShortSetup = false;
		private double ShortLimitLine;

		private bool Congestion = false;
		private int Congestion_Count = 0;

		private Rectangle myRec;
		private double Top_Con;
		private double Bot_Con;

		private bool temp_drawshort = false;
		private bool temp_drawlong = false;
		private int temp_shortbar;
		private int temp_longbar;

		private double[] my_index = {1,1.5, 2, 2.5, 3, 4, 5, 6, 7, 8};




		protected override void OnStateChange(){
			if (State == State.SetDefaults){
				Description									= @"My strategy2";
				Name										= "mypointstrate";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 3;
				
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
			}
			else if (State == State.Configure){ //called after user apply the strategy
				AddDataSeries("ES 09-20", Data.BarsPeriodType.Minute, 5);
				
				//SetProfitTarget(CalculationMode.Ticks, 20);
				//SetStopLoss(CalculationMode.Ticks, 12);
			}
			else if (State == State.DataLoaded){ //Called after data has been loaded
				AddChartIndicator(CCI(14));
			}
		}

		protected override void OnBarUpdate(){
			if (BarsInProgress == 0){
				if(CurrentBar < BarsRequiredToTrade){return;}

				if(Close[0]>=Low[ 1] && Close[0]<=High[ 1]){
					Congestion_Count++; //3
					if(Congestion_Count>=3){
						Top_Con = Close[0];
						Bot_Con = Close[0];
						for(int i=1;i<Congestion_Count;i++){
							if(Close[i]>Top_Con){Top_Con = Close[i];}
							if(Close[i]<Bot_Con){Bot_Con = Close[i];}
						}
						myRec = Draw.Rectangle(this, "Rectangle" + (CurrentBar-Congestion_Count-1), false, Congestion_Count-1, Top_Con, -30, Bot_Con, Brushes.White, Brushes.White, 3);
						Congestion = true;
					}//resize the zone if possible, and add it to the linked list, Congestion = true;}
				}
				else{
					Congestion_Count = 0;
					if(Congestion && (Close[0]>Top_Con || Close[0]<Bot_Con)){ //&& close outside the zone){
						Congestion = false;
					}
				}

				//bullish momentum - as price bar bottom clearing above the last swing high 


				if(ShortSetup && Low[0]>ShortLimitLine){ShortSetup = false;}
				if(LongSetup && High[0]<LongLimitLine){LongSetup = false;} //cancel limit order 

				if(High[0]-High[1 ]>High[ 1]-High[2] && High[ 1]-High[2]>High[2]-High[3] && High[2]-High[3]>0){
					Draw.Square(this, "Setup High"+ CurrentBar, true, 0, High[0]+2, Brushes.White);
					ShortSetup = true;
					ShortLimitLine = High[0];
					temp_drawshort = false;
					temp_shortbar = 0;
				}
				//else if(ShortSetup){
				//	if(Close[0]-Open[0]<0){// going down 
				if(ShortSetup){
					if(Close[0]-Open[0]<0&&temp_shortbar==0){temp_shortbar = CurrentBar;}
					else if(Low[0]<=Low[CurrentBar - temp_shortbar]-0.25 && temp_shortbar!=0){ 
						if(!temp_drawshort){
							int temp_holder = CurrentBar - temp_shortbar;
							Draw.Rectangle(this, "Rectangle down 1 " + CurrentBar, false, 1, High[temp_holder]+0.25, -30, Low[temp_holder]-0.25, Brushes.Red, Brushes.Red, 3);
							Draw.Rectangle(this, "Rectangle down 2 " + CurrentBar, false, 1, Low[temp_holder]-0.25, -30, Low[temp_holder]-0.25-9*(High[temp_holder]-Low[temp_holder]+2*0.25), Brushes.PaleGreen, Brushes.PaleGreen, 3);
							Draw.Line(this, "Line down" + CurrentBar, false, 1, Low[temp_holder]-0.25, -30, Low[temp_holder]-0.25, Brushes.Black, DashStyleHelper.Solid, 2);
							foreach (var item in my_index){
								Draw.Line(this, "Line down "+ item + " " + CurrentBar, false, 1, Low[temp_holder]-0.25-item*(High[temp_holder]-Low[temp_holder]+0.5), -30, Low[temp_holder]-0.25-item*(High[temp_holder]-Low[temp_holder]+0.5), Brushes.Black, DashStyleHelper.Solid, 2);
							}
						}
						temp_drawshort = true;
					}//limit order submit; set stoploss + 1tick}

				}

				if(Low[0]-Low[1 ]<Low[1 ]-Low[2] && Low[ 1]-Low[2]<Low[2]-Low[3] && Low[2]-Low[3]<0){
					Draw.Square(this, "Setup Low"+ CurrentBar, true, 0, Low[0]-2, Brushes.White);
					LongSetup = true; 
					LongLimitLine = Low[0];
					temp_drawlong = false;
					temp_longbar = 0;
				}
				if(LongSetup){
					if(Close[0]-Open[0]>0&&temp_longbar==0){temp_longbar=CurrentBar;}
					else if(High[0]>=High[CurrentBar - temp_longbar]+0.25  && temp_longbar!=0){
						if(!temp_drawlong){
							int temp_holder = CurrentBar - temp_longbar;
							Draw.Rectangle(this, "Rectangle up 1 " + CurrentBar, false, 1, High[temp_holder]+0.25, -30, Low[temp_holder]-0.25, Brushes.Red, Brushes.Red, 3);
							Draw.Rectangle(this, "Rectangle up 2 " + CurrentBar, false, 1,	High[temp_holder]+0.25 + 9*(High[temp_holder]-Low[temp_holder]+2*0.25), -30, High[temp_holder]+0.25, Brushes.PaleGreen, Brushes.PaleGreen, 3);
							Draw.Line(this, "Line up " + CurrentBar, false, 1, High[temp_holder]+0.25, -30, High[temp_holder]+0.25, Brushes.Black, DashStyleHelper.Solid, 2);
							foreach (var item in my_index){
								Draw.Line(this, "Line up " + item+ " " + CurrentBar, false, 1, High[temp_holder]+0.25+item*(High[temp_holder]-Low[temp_holder]+2*0.25), -30, High[temp_holder]+0.25+item*(High[temp_holder]-Low[temp_holder]+2*0.25), Brushes.Black, DashStyleHelper.Solid, 2);
							}
						}
						temp_drawlong = true;
					}//Limit order submit; set stoploss + 1tick}
				}
				//might have to look into trabar 

				//bullish momentum - as price bar bottom clearing above the last swing high 
					//use this as indicator for trend, 
				//swing(3)
				//if there are multiple green bar, you might want to wait for a stronger intrabar signal for confirmation 
				//can use anti climax as a exit signal, example if the anti climax formed in the bearish direction (about to reverse upwards) and you have an open position in the bear side, you can exit the position once it's formed 
				//price high unable to clear the last swing low - up
				//price low unable to clear the last swing high - down

				//price congestion - support and resistance 
				//as long as the bar closes within the range of the previous bar's highs and lows, we consider it's congesting 
				//when at least three consecutive bars close within the range of their respecitive preceding bar  
				/*
						 1		2	3
				    |    |		|	|
					[]	 []		[]  []
					|    |		|   |
				*/
				//highest close(3) and lowest close(1) - congesting zone 

				//if anti climax formed on bear side a few bars ago and failed then formed again most likely its gonna fail again 
				//if stop loss is hit but not clear limit line can consider reentering
				//you can use the anti climax to find the market bias, for example if a limit line is cleared, it shows that the market is going in that direction still hence strong market 
				//in a up trend if the anti climax going towards the short side is very strong, meaning that there might be a trend change, hence if theres a setup for going to the bull side, dont take it cus its risky 
				//use signs of price congestion to take your profit.
			}

			/*
			may get into "vriable risk" sizing when include support and resistance 
			lots of small loss, a big win
			stoploss behind lower lowers and higher low etc 
			//or 3 atr and close position when closed below it
			use HL,LH,LL,HH

			1:1 profit - one profit target, running stoploss at ent



			1:1 50% +
			4:1 20% +
			
			4 + 4 - 1 - 1 - 1 -1 -1 -1 -1 -1  


			take 25 percent off the trade when the trade reaches 1 times the resk and will move the stoploss to break even 

			able to accept 10 consecutive trades losses, 
			10 consecutive losses should not exceed a total 25 percent drawdown, 
			no more than 1- 2 percent of the portfolio should be put at risk 
			find amount of contracts based on /\
			
			assuming youre able to risk 30$, which means 6 points in MES, and the risk is 6 points, so you have 1 contract only

			assuming you have 1500$, each contract is 1000 so youre able to risk 500 max,
			with 2% of your account size, youre risking at most 30$
			if your trade is 6 points risk, 

			risk of trade = (entry price - stoploss price) * quantity traded *50 	
			RR ratio with win rates is the key 
			RRR 1:1 50%+, RRR 1.5:1 40%+, RRR 2:1 33%+ RRR 3:1 25%+ RRR 4:1 20%+
			
			*exit when another trade is forming in the opposite side and give that trade setup a try
			*dont reenter after filling a setup 
			*dont enter more than x times with a same setup 
			*maybe check with in trend trades
			*/
			else{return;}		
		}
		#region Helpers

		#endregion
	}
}

