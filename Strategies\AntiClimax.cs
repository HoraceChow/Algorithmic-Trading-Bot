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
		
		private double LongLimitLine;
		private double ShortLimitLine;
 		private double Accountsize;
		private Order myEntryOrder = null;
		private bool AllowEntry = true;

		private bool Congestion = false;
		private int Congestion_Count = 0;

		private Rectangle myRec;
		private double Top_Con;
		private double Bot_Con;

		protected override void OnStateChange(){
			if (State == State.SetDefaults){
				Description = "This trading bot is using the Anti Climax market pattern, where it trade based on price reversal/price exhaution. This trading bot also included risk management.";
				Name = "AntiClimax";
				Calculate = Calculate.OnBarClose;
				Instrumental = "ES 09-20";
				AccountName = "Playback101";
				Percent = 1.5;
				ContractTickPrice = 50;
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
				TraceOrders = false;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade = 3;
				IsInstantiatedOnEachOptimizationIteration = true;
			}
			else if (State == State.Configure){ //called after user apply the strategy
				AddDataSeries(Instrumental, Data.BarsPeriodType.Minute, 5);
				Account a = Account.All.First(t => t.Name == AccountName);
				Accountsize = a.Get(AccountItem.CashValue, Currency.UsDollar);
			}
			else if (State == State.DataLoaded){ //Called after data has been loaded
				AddChartIndicator(CCI(14));
			}
		}

		
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError){
			Account a = Account.All.First(t => t.Name == AccountName);
			double CurrentAccountsize = a.Get(AccountItem.CashValue, Currency.UsDollar);
			if(Accountsize-CurrentAccountsize>=Percent*0.03*Accountsize)
				AllowEntry = false;
			if(order.Name == "Long Entry" || order.Name == "Short Entry")
				myEntryOrder = order;
			if(myEntryOrder != null && myEntryOrder == order){
				if (myEntryOrder.OrderState == OrderState.Cancelled){
					myEntryOrder = null;
				}
			}	
		}

		protected override void OnBarUpdate(){
			if (BarsInProgress == 0){
				if(CurrentBar < BarsRequiredToTrade){return;}
				if(ToTime(Time[1]) <= 240000 && ToTime(Time[0]) >= 0){
					AllowEntry = true;
					Account a = Account.All.First(t => t.Name == AccountName);
				    Accountsize = a.Get(AccountItem.CashValue, Currency.UsDollar);
				}
				if(!AllowEntry){return;}
				
			/*--------------------------------------------------------------------------------------------------------------*/
				
				if(Close[0]>=Low[1] && Close[0]<=High[1]){
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
					
			/*--------------------------------------------------------------------------------------------------------------*/
				
				if(myEntryOrder!=null && myEntryOrder.Name == "Short Entry" && Low[0]>ShortLimitLine)
					CancelOrder(myEntryOrder); 
				if(myEntryOrder!=null && myEntryOrder.Name == "Long Entry" && High[0]<LongLimitLine)
					CancelOrder(myEntryOrder);

				if(High[0]-High[1]>High[1]-High[2] && High[1]-High[2]>High[2]-High[3] && High[2]-High[3]>0){
		
					Draw.Square(this, "Setup High"+ CurrentBar, true, 0, High[0]+2, Brushes.White);
				
					ShortLimitLine = High[0] + TickSize;
					double EntryLine = Low[0]-TickSize;
					double quantity = Accountsize*0.01*Percent/((ShortLimitLine-EntryLine)*ContractTickPrice);
					if(Position.MarketPosition == MarketPosition.Flat){
						if(ToTime(Time[0]) >= 73000 && ToTime(Time[0]) <= 155900){
							if(quantity>=0.75){
								SetProfitTarget(CalculationMode.Price, Instrument.MasterInstrument.RoundToTickSize(1.5*(EntryLine-(ShortLimitLine-EntryLine))));
								SetStopLoss(CalculationMode.Price, ShortLimitLine);
								EnterShortStopMarket((int)Math.Round(quantity),EntryLine, "Short Entry");
							}
						}
					}
				}
				if(Low[0]-Low[1]<Low[1]-Low[2] && Low[1]-Low[2]<Low[2]-Low[3] && Low[2]-Low[3]<0){

					Draw.Square(this, "Setup Low"+ CurrentBar, true, 0, Low[0]-2, Brushes.White);

					LongLimitLine = Low[0]-TickSize;
					double EntryLine = High[0]+TickSize;
					double quantity = Accountsize*0.01*Percent/((EntryLine-LongLimitLine)*ContractTickPrice);
					if(Position.MarketPosition == MarketPosition.Flat){
						if(ToTime(Time[0]) >= 73000 && ToTime(Time[0]) <= 155900){
							if(quantity>=0.75){
								SetProfitTarget(CalculationMode.Price, Instrument.MasterInstrument.RoundDownToTickSize(1.5*(EntryLine+(EntryLine-LongLimitLine))));
								SetStopLoss(CalculationMode.Price, LongLimitLine);
								EnterLongStopMarket((int)Math.Round(quantity),EntryLine, "Long Entry");
							}
						}	
					}
				}
				//might have to look into trabar 

				//bullish momentum - as price bar bottom clearing above the last swing high 
					//use this as indicator for trend, 
				//swing(3)
				//can use anti climax as a exit signal, example if the anti climax formed in the bearish direction (about to reverse upwards) and you have an open position in the bear side, you can exit the position once it's formed 
				//price high unable to clear the last swing low - up
				//price low unable to clear the last swing high - down
				//if anti climax formed on bear side a few bars ago and failed then formed again most likely its gonna fail again 

				//you can use the anti climax to find the market bias, for example if a limit line is cleared, it shows that the market is going in that direction still hence strong market 
				//in a up trend if the anti climax going towards the short side is very strong, meaning that there might be a trend change, hence if theres a setup for going to the bull side, dont take it cus its risky 
			}

			/*


			able to accept 10 consecutive trades losses7
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
			*/
			else{return;}		
		}
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Instrumental", GroupName = "NinjaScriptParameters", Order = 0)]
		public string Instrumental { get; set; }
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Account Name", GroupName = "NinjaScriptParameters", Order = 1)]
		public string AccountName { get; set; }
		[Range(1, 100), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Percent", GroupName = "NinjaScriptParameters", Order = 2)]
		public double Percent { get; set; }
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Contract Tick Price", GroupName = "NinjaScriptParameters", Order = 3)]
		public int ContractTickPrice { get; set; }
	}
}
