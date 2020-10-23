# Algorithmic-Trading-Bot
![Screenshot (9)](https://user-images.githubusercontent.com/46755190/97040692-4aaced00-153c-11eb-972a-215367f46bde.png)

Day trading requires a lot of emotional control and disciplines. Traders often have to think fast and make quick decisions while an entry setup is available. When a trader isn't able to contain their emotion, these emotion would usually impact their decision making and causing them to enter trades that aren't the best setups.

This project was inspired through personally experiencing multiple unfavourable trades and realizing the need for a better method to make quicker decision, accuratly spotting setups, and eliminating emotions.

Design and developed 3 different algorithmic trading bots with different setups, patterns and strategies. In addtion, developed an algorithm which able to determine market trends bias and find draw the trend channels. All trading bots uses different strategy and method of operation. The instrument that is used is the S&P 500 futures(ES).

## Challenges
This project was one of the most interesting project that I have done. It was a very fulfilling time but frustrating, especially when you thought you would've made great progress on an algorithmic trading bot, but only to find it was not profitable. Due to this reason, I was able to come up with loads of different variation of bots. The trading bots within this repository are backtest and shown profitable. The AntiClimax trading bot had the highest win rate, please check ou the explaination below!

## Usage
The trading bots are built based on the Ninjatrader trading platform. To use the trading bots, a brokerage that is compatible with Ninjatrader is required, and the file must be used within the Ninjatrader platform. 
For more information regarding some of the function that are used in the trading bot, please visit https://ninjatrader.com/support/helpGuides/nt8/

***DISCLAIMER: There is a risk of loss in forex, futures and options trading. Please be advise before use.***

## AntiClimax Trading Bot 
When the market is falling/raising with strong momentum, evenutally price begin to exhaustion and reverse in the opposite direction. The Anti climax pattern focus on trading price exchastion and indicate potential market reversal. The requirements for a long setup anticlimax pattern are a mirror to the short setup anticlimax pattern.

### Setup:
![Screenshot (15)](https://user-images.githubusercontent.com/46755190/97039808-ffdea580-153a-11eb-9411-f6d9223b4d8c.png)

Firstly, 3 consective bar are needed to be lower than the previous bar's low with an increasing distances. Each range(A,B,C) will get progressivly larger. This will show the rapid downward move in the market and potential price exhaustion.

![Screenshot (16)](https://user-images.githubusercontent.com/46755190/97041698-decb8400-153d-11eb-9f4d-d216d193a880.png)

Once the setup has been formed, wait for the next Green bar to form, it does not have to be the bar after the setup! When it appears like it have shown above, place a Buy market stop order 1 tick above the bar's high, and a stoploss 1 tick below the bar's low.
If the 3rd bar of the setup is a green bar, the stoploss and Buy market stop order can be placed on the 3rd bar's low and bar's high.

![Screenshot (17)](https://user-images.githubusercontent.com/46755190/97042354-e63f5d00-153e-11eb-8e81-dce58f613d22.png)

The 3rd bar's low of the setup serves as a limit line. While the setup has not been filled yet, any bar's high is below the limit line, the setup becomes invaild and no longer considered. The anticlimax pattern is essentially a power price thrust, there is a possibility that this price thrust will continue. If it continue, the limit line will be broken, proven that the setup is invaild and possiblily not reversing in price.

### Here it is in action:

![122557251_4584259951615388_3148439151456043905_n](https://user-images.githubusercontent.com/46755190/97043255-26eba600-1540-11eb-8b7b-651ea90adb2b.png)

### Features:
  * ***Risk management*** 
  
  To ensure a single losing trade does not fully bankrup the account's capital. It is suggested that each trades should only risk 1% to 2% of the account size. The trading bot is implemented with risk management of each trades only risking 1.5% of the account's capital.
  
  * ***1:1.5 Risk to reward ratio*** 
  
    ![122239327_787158372135874_6754269603419990388_n (2)](https://user-images.githubusercontent.com/46755190/97047804-826d6200-1547-11eb-9e2f-df30f28a8f21.jpg)

    Through backtesting over 100 trades, it was shown that the Anticlimax trading bot had a 47% with rate with 1:1.5 risk to reward ratio, each trade uses 1.5% of account size. Theoretically it has a 26.25% gain over 100 trades, and the trading bot was about to complete 100 trades over 4 weeks of day trading.
  
  * ***Daily stoploss*** 
  
  By implementing a daily stoploss, it could help preserving capital during a bad trading day. The maximum amount of account lost allow for each trading day is 4.5% of the account size. 


### Files
There are 3 different strategy and an algorithm
***More in depth explaination in each trading bot files***
* CCIWithPreditionEntry - This trading bot estimate the CCI value and enter trades based on the value crossover on CCI=100 or CCI=-100.
* mypointstrates - This trading bot uses the two legged pull back pattern with the Autotrendchannel's algorithm for trading with trade bias.
* Anticlimax(on going) - This trading bot use the anticlimax pattern, which focus price exhaustion trading.
* AutoTrendChannels - An algorithm algorithm that determines market bias, and find the most suitable trends channels, when implemented into trading bot that focus on trading with market bias, it would increase probability of winning trades while filtering bad trades.

