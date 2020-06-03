# Lykke.HftApi.Tools.GrpcStreamReader
Tool to read a grpc stream for the testing purpose. Using this tool, you can specify a GRPC server url, stream name and key 
(additional stream parameter, for example asset pair for orderbook stream)to read from. Received data will be displayed in the console.

## Run

To run this tool, you should have [.NetCore runtime](https://www.microsoft.com/net/download) installed on your machine.

To run the tool you need to type ```dotnet Lykke.HftApi.Tools.GrpcStreamReader.dll <options>``` in the console.

Awailable options:

```
-u <uri>. Hft api grpc url. Required
-n <stream name>. GRPC stream name. Required. Available values: Prices, Tickers, Orderbooks, Balances, Orders
-k <Stream key>. GRPC stream key parameter (for example asset pair id for orderbooks stream).
```

Run example:

```
dotnet Lykke.HftApi.Tools.GrpcStreamReader.dll -u https://hft-api-grpc-dev.lykkex.net -n Orderbooks -k ETHUSD
```
