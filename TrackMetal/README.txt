TrackMetal is a tax accounting program for calculating capital gains on bullion investments. 
It can handle many aspects of bullion investing that normal accounting programs, such as 
Quicken and Microsoft Money, fail badly at:

- Preserving lot identity during transfers between vaults, which may appear as a separate buy 
  and sell transaction
- Tracking the basis adjustment due to metal storage costs and other investing costs
- Preserving lot identity across "like-kind" exchanges - such as, for example, selling silver
  in one service and immediately buying it via another.
- Treating the buy-sell spread on a "like-kind" transaction as a basis adjustment (cost to move
  between services or vaults)
- Accounting for metal storage costs taken either by charging currency or by removing metal
  from the holding
- Understanding that the metal retains its identity when addressed in different accounting 
  units (ie. automatic conversion between troy oz and grams)

It produces a result file of capital gains transactions and also outputs a view of the current lots.

TrackMetal understands the QIF export formats used by several common bullion storage companies
(GoldMoney, BullionVault) and, through the use of auxiliary utilities, can convert them to 
tab-delimited .txt formats and then consume them. It automatically corrects for database and 
output format changes made by these services through time and fixes known issues in their 
extracts.

When run, it reads all .txt files in its working directory (except for any beginning with "tm-",
which are its own). File names should, by convention, be named <servicename>-<account>-<subtype>.txt 
(ex. Goldmoney-Joint-silver.txt). The subtype allows you to divide the transactions in whatever way 
is convenient - by metal or by transaction type, for example. This is helpful for adding in other costs,
such as wire transfer fees or shipping costs. TrackMetal combines all the transactions into a single 
transaction set, on which it operates, so the division does not matter to it. 

The .txt formats for BullionVault and GoldMoney are specific to those services (see the sample files)
and are produced by downloading reports from those services (see instructions below). Other types use 
a generic .txt file format as follows:

Date, Vault, Order ID, Type, Amount, Currency, Weight, WeightUnit, Metal, Status, Invoice, Invoice Date

The data should be tab-separated, with a header line. See the sample data files for more info.

After running, TrackMetal will output the following result files:

- tm-gains-<year>.txt: list of the capital gains recorded for that year
- tm-transactions.txt: a dump of the complete transaction history assembled from all the .txt files