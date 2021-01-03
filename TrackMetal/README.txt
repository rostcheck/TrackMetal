TrackMetal is a tax accounting program for calculating capital gains on physiccal and virtual assets
such as bullion and cryptocurrency investments. 
It can handle many aspects of physical investing that normal accounting programs, such as 
Quicken and Microsoft Money, fail badly at:

- Preserving lot identity during transfers between vaults, which may appear as a separate buy 
  and sell transaction
- Tracking the basis adjustment due to metal storage costs and other investing costs
- Preserving lot identity across "like-kind" exchanges - such as, for example, selling silver
  in one service and immediately buying it via another.
- Treating the buy-sell spread on a "like-kind" transaction as a basis adjustment (cost to move
  between services or vaults)
- Transferring cryptocurrency between services (while retaining the original lot open date)
- Accounting for metal storage costs taken either by charging currency or by removing metal
  from the holding
- Understanding that the metal retains its identity when addressed in different accounting 
  units (ie. automatic conversion between troy oz and grams)

It produces a result file of capital gains transactions and also outputs a view of the current lots.

TrackMetal understands the QIF export formats used by several common bullion storage companies
(GoldMoney, BullionVault) and, through the use of auxiliary utilities, can convert them to 
tab-delimited .txt formats and then consume them. It automatically corrects for database and 
output format changes made by these services through time and fixes known issues in their 
extracts. It can also accept input in spreadsheet format (see below)

When run, it reads all .txt files in its working directory (except for any beginning with "tm-",
which are its own). File names should, by convention, be named <servicename>-<account>-<subtype>.txt 
(ex. Goldmoney-Joint-silver.txt). The subtype allows you to divide the transactions in whatever way 
is convenient - by metal or by transaction type, for example. This is helpful for adding in other costs,
such as wire transfer fees or shipping costs. TrackMetal combines all the transactions into a single 
transaction set, on which it operates, so the division does not matter to it. 

The .txt formats for BullionVault and GoldMoney are specific to those services (see the sample files)
and are produced by downloading reports from those services (see instructions below). Other types use 
a generic .txt or .csv file format as follows:

Date, Vault, Order ID, Type, Amount, Currency, Weight, WeightUnit, Metal, Status, Invoice, Invoice Date, Item Type

The data should be tab-separated or comma-separated, with a header line. If using CSV, strings with commas
should be escaped with quotes("). Item type is optional; if set, it will restrict the 
matching to only match items of that same type. This is used for semi-numismatics, ex. item type of "1-oz Gold Maple Leaf",
or for cryptocurrencies, ex. item type of "BTC", and could be used in a similar way for other assets such as
trading cards. See the sample data files for more info.

After running, TrackMetal will output the following result files:

- tm-gains-<year>.txt: list of the capital gains recorded for that year
- tm-transactions.txt: a dump of the complete transaction history assembled from all the .txt files

It also prints out a report of all the open lots, with a summary of their history. History can be 
complex, as metal may be transferred between services and storage charges may have been applied to 
lots.


Importing Data from GoldMoney
-----------------------------
From within GoldMoney, do:

- From "My Holding Menu", select "Statements"

- Insure "Transactions Reports" is selected in the "Select report" dropdown

- Because GoldMoney's export report does not declare the type of metal or the account, you should 
  download a separate export for each metal type and account (ex. GoldMoney-Joint-Gold.txt):
    - Select the metal. 
    - Set the display date to a date before your account was opened. 
    - Leave "Show metal amount equivalents in this currency" set to "n/a"
    - Check the box for "Download report as a spreadsheet file"

- Run the report. It should open a spreadsheet in Excel or a compatible spreadsheet program. If an
  extra blank line appears at the top, delete it.

- From within the spreadsheet, Export or Save As the data to the TrackMetal working directory as a 
  tab-separated file (.txt) with naming convention like "Goldmoney-<accountname>-<metal>.txt" 
  (replacing accountname and metal appropriately).

- Continue doing exports for any other metals


Importing Data from BullionVault
-----------------------------
Importing data from BullionVault is more complex because the transactions, fees, and original 
goldgram transactions all come from different sources. They are placed into separate text files
and TrackMetal will merge the data together when it runs.

BullionVault does not provide a sufficient mechanism for downloading its historical data within
the web interface. For that reason, you must use the GetBullionVaultData utility program - available
separately at https://bitbucket.org/davidrostcheck/getbullionvaultdata. This program wil use
BullionVault's published API to query historical data and save it to a file. It requires you to
input yout BullionVault account name and password. 

After GetBullionVaultData has run, it will produce a tab-delimited .txt file named BullionVault.txt.
Rename this file to BullionVault-<accountname>-all.txt (ex. BullionVault-Main-all.txt) and move it 
to the TrackMetal working directory. TrackMetal will import it when it runs.

Additionally, the BullionVault API does not provide a way to get storage fees. You must add the 
storage fees from an additional source. There are two ways to do this:

- You can manually enter them in a spreadsheet that corresponds to the spreadsheet format used for
  inport of BullionVault files to TrackMetal (see below), or

- If you have the fees entered in financial software that can output a QIF file, you can output a
  QIF file and use the utility GetBullionVaultDataFromQIF to convert it to a tab-delimited file
  suitable for import to TrackMetal. This may require modifying the utility's code.

In either case, the storage fees should be saved to an additional file in the TrackMetal working
directory, named BullionVault-<accountname>-fees.txt (ex. BullionVault-Main-fees.txt). 

Finally, BullionVault gives you one free gram of gold when you open your account, and this is not
reflected in the transactions. To account for this, create a tab-delimited file named 
BullionVault-<accountname>-gold-additional.txt (ex. BullionVault-Main-gold-additional.txt) and 
place it in the TrackMetal working directory.

The file format expected for the tab-delimited BullionVault data is:

OrderDateTime, OrderID, Action, Vault, Value, ClientTransRef, Currency, Metal, GoodTil, LastModified, 
PricePerKg, OrderType, Status, Quantity, QtyFilled, Commission, Consideration, TradeType