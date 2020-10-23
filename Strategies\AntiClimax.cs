/*
This is the most effecient and highest probability trading bot out of the list. 

This is trading bot uses the anticlimx market pattern, a visual explaination could be viewed on github.com/HoraceChow/Algorithmic-Trading-Bot
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

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies{
	public class AntiClimax : Strategy{
		
		private double LongLimitLine;
		private bool LongSetup = false;
		private double ShortLimitLine;
		private bool ShortSetup = false;
 		private double Accountsize;
		private Order myEntryOrder = null;
		private bool AllowEntry = true;

		protected override void OnStateChange(){
			if (State == State.SetDefaults){
				Description = @"Trading using the AntiClimax Pattern";
				Name = "AntiClimax";
				Calculate = Calculate.OnBarClose;
				Instrumental = "ES 09-20";  //Trading the S&P500 futures instrumental
				AccountName = "Playback101"; //Using backtesting account
				Percent = 1.5; //Each trade would be consistance and use 1.5% of overall account size 
				ContractTickPrice = 50; //Each Point in the S&P500 = 50 USD dollars 
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
			else if (State == State.Configure){ //called after User applying the trading bot
				AddDataSeries(Instrumental, Data.BarsPeriodType.Minute, 5); //adding data instrumental
				
				//Attaining Account size
				Account a = Account.All.First(t => t.Name == AccountName); 
				Accountsize = a.Get(AccountItem.CashValue, Currency.UsDollar);
			}
			else if (State == State.DataLoaded){ //Called after data has been loaded
				AddChartIndicator(CCI(14));
			}
		}
		//Runs whenever an order has been entered, each profit target, or hit stop loss
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time){
			if(execution.Order.Name == "Profit target" || execution.Order.Name == "Stop loss")
				myEntryOrder = null; //Removing myEntryOrder for reference
		}
		
		//Runs whenever an order statues has been updated
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string nativeError){
			//Initalizing Reference of Order
			if(order.Name == "Long Entry" || order.Name == "Short Entry")
				myEntryOrder = order;
			
			//Removing myEntryOrder reference once order has been cancelled
			if(myEntryOrder != null && myEntryOrder == order){
				if (myEntryOrder.OrderState == OrderState.Cancelled)
					myEntryOrder = null;
			}	
		}

		//Runs each time a new bar is formed
		protected override void OnBarUpdate(){
			if (BarsInProgress == 0){ //Runs every 5 mins, since bar type is set to 5 mins
				if(CurrentBar < BarsRequiredToTrade){return;} 
				
				//Runs when day changed
				if(ToDay(Time[1]) != ToDay(Time[0])){ //If the previous and current bar does not have equal date
					a = Account.All.First(t => t.Name == AccountName); //Update account size
					Accountsize = a.Get(AccountItem.CashValue, Currency.UsDollar);
					AllowEntry = true; 
					ShortSetup = false;
					LongSetup = false;
				}

				//Setting daily stoploss, lossing no more than specify percent per day.
				Account a = Account.All.First(t => t.Name == AccountName);
				double CurrentAccountsize = a.Get(AccountItem.CashValue, Currency.UsDollar);
				if(Accountsize-CurrentAccountsize>=Percent*0.03*Accountsize)
					AllowEntry = false;
			
				//Daily stoploss
				if(!AllowEntry)
					return;

				//Cancel Order once Short setup is no longer vaild
				if((ShortSetup || (myEntryOrder!=null && myEntryOrder.Name == "Short Entry")) && Low[0]>ShortLimitLine){
					ShortSetup = false;
					if(myEntryOrder!=null && myEntryOrder.Name == "Short Entry")
						CancelOrder(myEntryOrder); //OnOrderUpdate will run, changing order
				}

				//Cancel Order once Long setup is no longer vaild
				if((LongSetup || (myEntryOrder!=null && myEntryOrder.Name == "Long Entry")) && High[0]<LongLimitLine){
					LongSetup = false;
					if(myEntryOrder!=null && myEntryOrder.Name == "Long Entry")
						CancelOrder(myEntryOrder); //OnOrderUpdate will run, changing order
				}  

				//Anti climax short setup formed
				if(High[0]-High[1]>High[1]-High[2] && High[1]-High[2]>High[2]-High[3] && High[2]-High[3]>0){
					ShortLimitLine = High[0] + TickSize;
					ShortSetup = true;
				}
				//Enter short stop market once Red bar has form while setup is still vaild
				if(ShortSetup && Close[0]-Open[0]<0){ //if setup is vaild and Red bar has formed 
					if(Position.MarketPosition == MarketPosition.Flat){ //No active positions opened

						//reset short setup if there exist one 
						if(myEntryOrder != null && myEntryOrder.Name == "Short Entry")
							CancelOrder(myEntryOrder); 
						if(myEntryOrder == null){
							double EntryLine = Low[0]-TickSize;
							double Stoploss = High[0]+TickSize;
							// attaining amount of contract to purchase, ensuring traindg no more than specify percentage of account size
							double quantity = Accountsize*0.01*Percent/((Stoploss-EntryLine)*ContractTickPrice); 
							if(ToTime(Time[0]) >= 70000 && ToTime(Time[0]) <= 155900){ //only allow trading between market open, 7a.m. - 3:59p.m.
								if(quantity>=0.75){ 
									//Set profit target as 1.5*stoploss, allowing 1:1.5 risk to reward ratio
									SetProfitTarget(CalculationMode.Price, Instrument.MasterInstrument.RoundToTickSize(EntryLine-1.5*(Stoploss-EntryLine))); 
									SetStopLoss(CalculationMode.Price, Stoploss);
									EnterShortStopMarket(0, true, (int)Math.Round(quantity),EntryLine, "Short Entry"); 
								}
							}
						}
					}
					ShortSetup = false;
				}

				//Anti climax long setup formed
				if(Low[0]-Low[1]<Low[1]-Low[2] && Low[1]-Low[2]<Low[2]-Low[3] && Low[2]-Low[3]<0){ 
					LongLimitLine = Low[0]-TickSize;
					LongSetup = true;
				}
				//Enter short stop market once Green bar has form while setup is still vaild
				if(LongSetup && Close[0]-Open[0]>0){  //if setup is vaild and Green bar has formed 
					if(Position.MarketPosition == MarketPosition.Flat){ //no active position opened

						//reset short setup if there exist one
						if(myEntryOrder != null && myEntryOrder.Name == "Long Entry")
							CancelOrder(myEntryOrder);
						if(myEntryOrder == null){
							double EntryLine = High[0]+TickSize;
							double Stoploss = Low[0]-TickSize;
							// attaining amount of contract to purchase, ensuring traindg no more than specify percentage of account size
							double quantity = Accountsize*0.01*Percent/((EntryLine-Stoploss)*ContractTickPrice);
							if(ToTime(Time[0]) >= 70000 && ToTime(Time[0]) <= 155900){ //only allow trading between market open, 7a.m. - 3:59p.m.
								if(quantity>=0.75){
									//Set profit target as 1.5*stoploss, allowing 1:1.5 risk to reward ratio
									SetProfitTarget(CalculationMode.Price, Instrument.MasterInstrument.RoundDownToTickSize(EntryLine+1.5*(EntryLine-Stoploss)));
									SetStopLoss(CalculationMode.Price, Stoploss);
									EnterLongStopMarket(0, true, (int)Math.Round(quantity),EntryLine, "Long Entry");
								}	
							}
						}
					}
					LongSetup = false;
				}
			}
			else{return;}		
		}
		
		//Displayed properties
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
