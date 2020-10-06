# Algorithmic-Trading-Bot
Day trading requires a lot of emotional control and disciplines. Traders often have to think fast and make quick decisions while an entry setup is available. When a trader isn't able to contain their emotion, these emotion would usually impact their decision making and causing them to enter trades that aren't the best setup.

This project was inspired through personally experiencing multiple unfavourable trades and realizing the need for a better method to make quicker decision, accuratly spotting setups, and eliminating emotions.

Design and developed 3 different algorithmic trading bots with different setups, patterns and strategies. In addtion, developed an algorithm which able to determine market trends bias and find draw the trend channels. All trading bots uses different strategy and method of operation. The instrument that is used is the S&P 500 futures(ES).

## Usage
The trading bots are built based on the Ninjatrader trading platform. To use the trading bots, a brokerage that is compatible with Ninjatrader is required, and the file must be used within the Ninjatrader platform. 

## Files
There are 3 different strategy and an algorithm
***More in depth explaination in each trading bot files***
* CCIWithPreditionEntry - This trading bot estimate the CCI value and enter trades based on the value crossover on CCI=100 or CCI=-100.
* mypointstrates - This trading bot uses the two legged pull back pattern with the Autotrendchannel's algorithm for trading with trade bias.
* Anticlimax(on going) - This trading bot use the anticlimax pattern, which focus price exhaustion trading.
* AutoTrendChannels - An algorithm algorithm that determines market bias, and find the most suitable trends channels, when implemented into trading bot that focus on trading with market bias, it would increase probability of winning trades while filtering bad trades.

## On going 
Currently completing AntiClimax trading bot, still need to implement
* Daily stoploss
* risk management
* fixing up syntax and algorithm 
