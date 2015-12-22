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



