using CowAuctionSmall.Infra.Persistence;
using CowAuctionSmall.Application.AnimalHistory;
using CowAuctionSmall.Infra.Api;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Domain.AnimalHistory;
using CowAuctionSmall.Domain.Settings;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;
class Program {
 static int checks;
 static void Check(bool ok,string label) { if(!ok) throw new Exception(label); checks++; }
 static string Xml(string items) => "<response><header><resultCode>00</resultCode></header><body><items>"+items+"</items></body></response>";
 static string Item(string date,string type,string owner) => $"<item><infoType>2</infoType><regYmd>{date}</regYmd><regType>{type}</regType><farmerNm>{owner}</farmerNm></item>";
 [STAThread] static void Main(string[] args) {
  if (args.Length == 3 && args[0] == "--live") { LiveChecks.Run(args[1], args[2]).GetAwaiter().GetResult(); return; }
  if (args.Length == 1 && args[0] == "--scroll-offline") {
   NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
   _ = System.Windows.Application.Current ?? new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
   try { ScrollChecks.Run(); }
   catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
   return;
  }
  RunChecks().GetAwaiter().GetResult(); MoreChecks.Run().GetAwaiter().GetResult();
  if (args.Length == 1 && args[0] == "--offline") return;
  UiChecks.Run();
 }
 static async Task RunChecks() {
  NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
  string first=Item("20260101","전산등록","TEST_OWNER_A"), second=Item("20260201","양수","TEST_OWNER_B");
  var result=AnimalHistoryParser.ParseAnimalHistory(Xml(second+first+first));
  Check(result.Status==HistoryStatus.Success && result.Entries.Count==2,"parse dedupe");
  Check(result.Entries[0].Owner=="TEST_OWNER_A","sort");
  Check(AnimalHistoryNoteFormatter.FormatAnimalHistoryNote(result)=="TEST_OWNER_A → TEST_OWNER_B(양수 2026-02-01)","format");
  Check(AnimalHistoryNoteFormatter.FormatAnimalHistoryNote(AnimalHistoryParser.ParseAnimalHistory(Xml(first)))=="","single");
  Check(AnimalHistoryNoteFormatter.FormatAnimalHistoryNote(AnimalHistoryParser.ParseAnimalHistory(Xml(first+Item("20260101","양수","TEST_OWNER_B"))))=="","ambiguous date");
  Check(AnimalHistoryNoteFormatter.FormatAnimalHistoryNote(AnimalHistoryParser.ParseAnimalHistory(Xml(first+Item("20260201","양수","TEST_OWNER_A"))))=="","same name");
  Check(AnimalHistoryParser.ParseAnimalHistory(Xml("")).Status==HistoryStatus.Success,"empty success");
  foreach(string bad in new[]{"bad", "<response><resultCode>00</resultCode></response>",Xml(Item("wrong","양수","A")),Xml(Item("20260201","양수","")),"<!DOCTYPE x [<!ENTITY a SYSTEM 'file:///no'>]><x>&a;</x>"}) Check(AnimalHistoryParser.ParseAnimalHistory(bad).Status==HistoryStatus.InvalidResponse,"invalid XML/fields");
  Check(AnimalHistoryParser.ParseAnimalHistory("<r><resultCode>30</resultCode></r>").Status==HistoryStatus.AuthenticationError,"auth");
  Check(AnimalHistoryParser.ParseAnimalHistory("<r><resultCode>22</resultCode></r>").Status==HistoryStatus.QuotaExceeded,"quota");
  var cow=new gValues{Note="original",NoteChangeOwner="history",TraceNumber="000000000000001"};
  Check(cow.DisplayNote=="original / history" && cow.Note=="original","composite preserves note");
  Check(cow.Clone().NoteChangeOwner=="history" && cow.Clone().TraceNumber==cow.TraceNumber,"clone");
  var copy=cow.Clone();copy.NoteChangeOwner="new";
  Check(!cow.Equals(copy) && cow.UpdateSignature()!=copy.UpdateSignature(),"change detected");
  cow.Note="-";Check(cow.DisplayNote=="history","history only");cow.NoteChangeOwner="";Check(cow.DisplayNote=="","empty");
  string dir=Path.Combine(Path.GetTempPath(),"HistoryFixture_"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
  string configPath=Path.Combine(dir,"users.XML");
  File.WriteAllText(configPath,"<Users><AnimalTrace><IsAnimalProductsTraceability>Y</IsAnimalProductsTraceability><ServiceKey> fake%2Bkey%3D </ServiceKey><Address>https://ignored.test</Address><DailyLimit>999</DailyLimit></AnimalTrace></Users>");
  var parsedSettings=new CowAuctionSmall.Infra.XmlSettings.UserXmlParser().ParseXml(configPath).AnimalTrace;
  Check(parsedSettings.Enabled && parsedSettings.ServiceKey=="fake%2Bkey%3D","XML key only");
  Check(parsedSettings.Address==new AnimalTraceSettings().Address && parsedSettings.DailyLimit==0 && parsedSettings.RequestIntervalMs==1000,"source defaults ignore old XML settings");
  File.WriteAllText(configPath,"<Users><AnimalTrace><IsAnimalProductsTraceability>Y</IsAnimalProductsTraceability><ServiceKey> </ServiceKey></AnimalTrace></Users>");
  Check(!new CowAuctionSmall.Infra.XmlSettings.UserXmlParser().ParseXml(configPath).AnimalTrace.Enabled,"blank key disables lookup");
  var missingKeyHandler=new FakeHandler(Xml(""));
  foreach(string flag in new[]{"N","","invalid"}) {
   File.WriteAllText(configPath,"<Users><AnimalTrace>"+(flag.Length==0 ? "" : "<IsAnimalProductsTraceability>"+flag+"</IsAnimalProductsTraceability>")+"<ServiceKey>fake</ServiceKey></AnimalTrace></Users>");
   var disabledUser=new CowAuctionSmall.Infra.XmlSettings.UserXmlParser().ParseXml(configPath);
   Check(disabledUser.AnimalTrace.IsAnimalProductsTraceability=="N" && !disabledUser.AnimalTrace.Enabled,"trace flag defaults off: "+flag);
  }
  using(var disabledCache=new DailyCacheService(Path.Combine(dir,"disabled.db"))) {
   await disabledCache.EnsureTodayAsync();
   string saved=JsonConvert.SerializeObject(result);
   await disabledCache.SaveCacheAsync("animal_history","000000000000001",saved,disabledCache.Epoch);
   var disabledHandler=new FakeHandler(Xml(first+second));
   using var disabledService=new AnimalHistoryService(disabledCache,new AnimalHistoryClient(new HttpClient(disabledHandler)));
   disabledService.Configure(new AnimalTraceSettings{Enabled=false,ServiceKey="fake"});
   Check(await disabledService.GetAnimalHistoryAsync("000000000000001")=="","disabled hides cached history");
   Check(await disabledService.GetAnimalHistoryAsync("000000000000002",true)=="" && disabledHandler.Calls==0,"disabled manual refresh no HTTP");
   Check(await disabledCache.GetCacheAsync("animal_history","000000000000001")==saved,"disabled retains existing daily cache");
   Check(await disabledCache.GetCacheAsync("animal_history","000000000000002")==null,"disabled does not save new history");
  }
  using(var missingKeyClient=new AnimalHistoryClient(new HttpClient(missingKeyHandler))) {
   Check((await missingKeyClient.FetchAnimalHistoryAsync(new AnimalTraceSettings(),"000000000000001",CancellationToken.None)).Status==HistoryStatus.Disabled && missingKeyHandler.Calls==0,"missing key no HTTP");
  }
  string path=Path.Combine(dir,"cache.db"); var clock=new DateTimeOffset(2026,9,15,3,0,0,TimeSpan.Zero);
  int days=0;
  using(var cache=new DailyCacheService(path,()=>clock)) {
   cache.DayChanged+=()=>days++;
   Check(await cache.EnsureTodayAsync(),"create");
   string epoch=cache.Epoch;
   Check(await cache.SaveCacheAsync("animal_history","000000000000001","{\"marker\":\"PRIVATE_TEST_MARKER\"}",epoch),"save");
   Check((await cache.GetCacheAsync("animal_history","000000000000001"))!.Contains("PRIVATE_TEST_MARKER"),"read");
   using(var contender=new DailyCacheService(path,()=>clock)) Check(!await contender.EnsureTodayAsync(),"exclusive ownership");
   clock=clock.AddDays(1); Check(await cache.EnsureTodayAsync() && days==1,"rollover");
   Check(await cache.GetCacheAsync("animal_history","000000000000001")==null,"expired gone");
   Check(!await cache.SaveCacheAsync("animal_history","000000000000001","{}",epoch),"stale response blocked");

   Check(await cache.SaveCacheAsync("control","test","{}",cache.Epoch),"control same table");
  }
   Check(!Encoding.UTF8.GetString(File.ReadAllBytes(path)).Contains("PRIVATE_TEST_MARKER"),"secure delete marker");
  using(var cache=new DailyCacheService(path,()=>clock)) {
   Check(await cache.GetCacheAsync("control","test")=="{}","same day reopen");
   clock=clock.AddDays(-1);Check(await cache.GetCacheAsync("control","test")==null,"clock backwards clears");
  }
  var settings=new AnimalTraceSettings{Enabled=true,Address="https://example.test/trace",DailyLimit=10,ServiceKey="fake%2Bkey%3D"};
  var handler=new FakeHandler(Xml(first+second));
  using(var cache=new DailyCacheService(Path.Combine(dir,"history.db"),()=>clock)) {
   using(var service=new AnimalHistoryService(cache,new AnimalHistoryClient(new HttpClient(handler)))) {
    service.Configure(settings);
    var notes=await Task.WhenAll(Enumerable.Range(0,8).Select(_=>service.GetAnimalHistoryAsync("000000000000001")));
    Check(handler.Calls==1 && notes.All(n=>n.Contains("TEST_OWNER_B")),"concurrent dedupe");
    Check(handler.LastQuery.Contains("optionNo=2") && !handler.LastQuery.Contains("%252B"),"option encoding");
    await service.GetAnimalHistoryAsync("000000000000001");Check(handler.Calls==1,"memory reuse");
    await service.GetAnimalHistoryAsync("000000000000001",true);Check(handler.Calls==1,"manual cooldown");
    Check(await service.GetAnimalHistoryAsync("bad")=="" && handler.Calls==1,"bad number no call");
   }
   var secondHandler=new FakeHandler(Xml(""));
   using(var service=new AnimalHistoryService(cache,new AnimalHistoryClient(new HttpClient(secondHandler)))) {
    service.Configure(settings); Check((await service.GetAnimalHistoryAsync("000000000000001")).Contains("TEST_OWNER_B") && secondHandler.Calls==0,"DB reuse");
    await service.GetAnimalHistoryAsync("000000000000002"); await service.GetAnimalHistoryAsync("000000000000002");Check(secondHandler.Calls==1,"empty reuse");
    clock=clock.AddDays(1);await cache.EnsureTodayAsync();Check(!service.TryGetNote("000000000000001",out _),"memory rollover");
   }
  }
  var quotaHandler=new FakeHandler(Xml(first)); settings.DailyLimit=1;
  using(var cache=new DailyCacheService(Path.Combine(dir,"quota.db"),()=>clock)) {
   using(var service=new AnimalHistoryService(cache,new AnimalHistoryClient(new HttpClient(quotaHandler)))) {
    service.Configure(settings);await service.GetAnimalHistoryAsync("000000000000001");await service.GetAnimalHistoryAsync("000000000000002");Check(quotaHandler.Calls==1,"budget cap");
   }
   var restartHandler=new FakeHandler(Xml(first));
   using(var service=new AnimalHistoryService(cache,new AnimalHistoryClient(new HttpClient(restartHandler)))) {service.Configure(settings);await service.GetAnimalHistoryAsync("000000000000003");Check(restartHandler.Calls==0,"budget persisted");}
  }
  string unlimitedPath=Path.Combine(dir,"uncapped.db");
  string budgetKey="animal-trace:"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("Config/animalTrace.local.json")));
  settings.DailyLimit=new AnimalTraceSettings().DailyLimit;
  using(var cache=new DailyCacheService(unlimitedPath,()=>clock)) {
   await cache.EnsureTodayAsync();
   await cache.SaveCacheAsync("control",budgetKey,"{\"Count\":100,\"Stop\":\"\"}",cache.Epoch);
   var handler101=new FakeHandler(Xml(first));
   using(var service=new AnimalHistoryService(cache,new AnimalHistoryClient(new HttpClient(handler101)))) {
    service.Configure(settings);await service.GetAnimalHistoryAsync("000000000000004");
    Check(handler101.Calls==1,"default permits request after persisted 100");
    Check((int)Newtonsoft.Json.Linq.JObject.Parse((await cache.GetCacheAsync("control",budgetKey))!)["Count"]! ==101,"uncapped requests still counted");
   }
  }
  using(var cache=new DailyCacheService(unlimitedPath,()=>clock)) {
   var handler102=new FakeHandler(Xml(first));
   using var service=new AnimalHistoryService(cache,new AnimalHistoryClient(new HttpClient(handler102)));
   service.Configure(settings);await service.GetAnimalHistoryAsync("000000000000005");
   Check(handler102.Calls==1,"uncapped requests continue after restart");
  }
  using(var connection=new SqliteConnection("Data Source="+path)) {connection.Open();using var cmd=connection.CreateCommand();cmd.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";Check((long)cmd.ExecuteScalar()! ==1,"one table");}
  Console.WriteLine($"PASS {checks} checks; synthetic data only; no live HTTP");
 }
}
class FakeHandler:HttpMessageHandler {
 readonly string xml;public int Calls;public string LastQuery="";
 public FakeHandler(string response){xml=response;}
 protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){Interlocked.Increment(ref Calls);LastQuery=request.RequestUri!.Query;await Task.Delay(80,token);return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(xml)};}
}
