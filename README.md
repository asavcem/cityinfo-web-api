# CityInfo Web API

Şehirleri ve şehirlerdeki ilgi noktalarını yönetmek için geliştirdiğim bir ASP.NET Core 10 eğitim projesi. Veritabanı kullanan REST endpoint'leri, DTO eşlemeyi, repository üzerinden veri erişimini ve birlikte uygulanabilen arama ve filtreleme koşullarını örnekliyor.

Önceki derslerde oluşturduğum CityInfo API'yi, Kevin Dockx'un Pluralsight eğitimiyle geliştirdim. [ASP.NET Core 10 Web API Essentials: Supporting Databases, Searching, Filtering, and Paging](https://www.pluralsight.com/courses/asp-dot-net-core-10-web-api-essentials-supporting-databases-searching-filtering-paging)

**Proje durumu.** İlk üç bölümü tamamladım ve dördüncü bölümde arama ile filtrelemeyi uyguladım. Uygulamalı çalışma bu aşamada sona erdi. Paging ve ayrı bir service layer kursun kalan konuları arasında.

## Uygulanan özellikler

- EF Core ve SQLite ile entity ilişkileri, üç migration ve seed data.
- Repository interface'leri ve scoped dependency injection.
- Async sorgular, `Include` ile ilişkili verileri isteğe bağlı yükleme ve `AsNoTracking` ile salt okunur sorgular.
- AutoMapper ile entity ve DTO eşleme.
- İlgi noktaları için oluşturma, okuma, tam güncelleme, kısmi güncelleme ve silme.
- Validation, HTTP durum kodları ve oluşturulan kaynaklar için `Location` header'ı.
- `ExecuteUpdateAsync` ile toplu açıklama güncellemesi ve repository içinde `ExecuteDeleteAsync` örneği.
- Şehir listesi sorgusuna iletilen request cancellation.
- Şehir adına göre tam eşleşmeli filtreleme, ad ve açıklamada metin arama.
- JSON ve XML yanıtları ile Development ortamında OpenAPI JSON belgesi.

## Kullanılan teknolojiler

.NET 10 · ASP.NET Core Controllers · Entity Framework Core 10 · SQLite · AutoMapper · System.Text.Json JSON Patch

## Yerelde çalıştırma

.NET 10 SDK gerekir. PowerShell'de depo kökünden başlayarak aşağıdaki komutları çalıştır.

```powershell
dotnet restore CityInfo.slnx
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Set-Location CityInfo.API
dotnet ef database update
dotnet run --launch-profile http
```

Yerel EF aracı, projedeki EF Core sürümüne sabitlenmiştir. SQLite'ın göreli dosya yolunun aynı konumu göstermesi için migration ve başlatma komutlarını `CityInfo.API` klasöründe çalıştır. Migration, üç şehir ve altı ilgi noktası içeren `CityInfo.db` dosyasını oluşturur.

- Şehir listesi `http://localhost:5124/api/cities` adresinde.
- OpenAPI JSON belgesi Development ortamında `http://localhost:5124/openapi/v1.json` adresinde.
- Hazır HTTP istekleri [CityInfo.API.http](CityInfo.API/CityInfo.API.http) dosyasında.

HTTPS kullanmak için `dotnet dev-certs https --trust` komutunu çalıştır, uygulamayı `dotnet run --launch-profile https` ile başlat ve istek dosyasındaki base URL'yi `https://localhost:7150` olarak güncelle.

Development ortamındaki connection string yerel bir SQLite dosyasına işaret eder. Dosyayı farklı bir konuma almak için `ConnectionStrings__CityInfoDBConnectionString` ortam değişkenini ayarla. Development dışındaki ortamlarda bu ayarı ayrıca belirt.

## API'yi deneme

```powershell
Invoke-RestMethod 'http://localhost:5124/api/cities'
Invoke-RestMethod 'http://localhost:5124/api/cities?name=Paris'
Invoke-RestMethod 'http://localhost:5124/api/cities?searchQuery=tower'
Invoke-RestMethod 'http://localhost:5124/api/cities?name=Paris&searchQuery=tower'
Invoke-RestMethod 'http://localhost:5124/api/cities/3?includePointsOfInterest=true'
```

`name` parametresi, baştaki ve sondaki boşluklar temizlendikten sonra tam eşleşme arar. `searchQuery` de aynı şekilde temizlenir ve şehir adında veya varsa açıklamasında aranır. İki parametreyi birlikte verdiğinde koşullar AND ile birleşir. Sonuçlar şehir adına göre sıralanır. Büyük ve küçük harf davranışı, SQLite'ın kullandığı metin karşılaştırmalarına bağlıdır.

| HTTP metodu | Route | İşlem | Başarılı yanıt |
| --- | --- | --- | --- |
| GET | `/api/cities` | Şehirleri listele, filtrele veya ara | 200 |
| GET | `/api/cities/{cityId}` | Bir şehri getir, isteğe bağlı olarak ilgi noktalarını ekle | 200 |
| GET | `/api/cities/{cityId}/pointsofinterest` | Bir şehrin ilgi noktalarını listele | 200 |
| GET | `/api/cities/{cityId}/pointsofinterest/{id}` | Bir ilgi noktasını getir | 200 |
| POST | `/api/cities/{cityId}/pointsofinterest` | İlgi noktası oluştur | 201 ve `Location` |
| PUT | `/api/cities/{cityId}/pointsofinterest/{id}` | İlgi noktasını tamamen güncelle | 204 |
| PATCH | `/api/cities/{cityId}/pointsofinterest/{id}` | JSON Patch ile kısmi güncelleme yap | 204 |
| DELETE | `/api/cities/{cityId}/pointsofinterest/{id}` | İlgi noktasını sil | 204 |
| PUT | `/api/cities/{cityId}/pointsofinterest/bulk` | Açıklamaları toplu güncelle | 200 ve `updatedCount` |

Oluşturma ve tam güncelleme isteklerinde `name` ve isteğe bağlı `description` kullanılır. Toplu güncelleme `newDescription` alanını alır. Aşağıdaki PATCH isteğinin content type değeri `application/json-patch+json` olmalıdır.

```json
[{ "op": "replace", "path": "/name", "value": "Updated attraction" }]
```

## Doğrulama

Depo kökünde aşağıdaki komutları çalıştır.

```powershell
dotnet build CityInfo.slnx
dotnet build CityInfo.slnx -c Release
./scripts/Verify-Api.ps1
```

Smoke test betiği API'yi derler, migration'larla geçici bir SQLite veritabanı kurar ve yerel bir API süreci başlatır. Arama, filtreleme, okuma, CRUD, validation, toplu güncelleme ve OpenAPI isteklerini kontrol ettikten sonra süreci kapatır. Log dosyaları ve test veritabanı, betiğin ekrana yazdığı geçici klasörde kalır.

1 Kasım 2026'da Debug ve Release derlemeleri uyarı ve hata olmadan tamamlandı. Migration ile sıfırdan kurulan veritabanında 24 HTTP smoke test başarılı oldu. NuGet taraması, o tarihte kullanılan kaynaklardaki doğrudan ve dolaylı paket bağımlılıklarında bilinen bir güvenlik açığı bildirmedi.

## Proje yapısı

```text
CityInfo.API/
  Controllers/   HTTP endpoint'leri
  DbContexts/    EF Core yapılandırması ve seed data
  Entities/      Veritabanı entity'leri ve ilişkileri
  Migrations/    Şema ve seed data değişiklikleri
  Models/        İstek ve yanıt DTO'ları
  Profiles/      AutoMapper eşlemeleri
  Services/      Repository sözleşmesi, implementasyonu ve e-posta örnekleri
scripts/         API smoke test betiği
.config/         Yerel EF CLI manifest dosyası
```

İstekler controller → repository → EF Core → SQLite akışını izler. AutoMapper, entity'ler ile DTO'lar arasındaki eşlemeyi yapar.

## Kapsam

Bu proje, kurs boyunca uyguladığım konuları bir araya getiren bir portföy örneği. Authentication, authorization policy'leri, paging ve ayrı bir service layer uygulama kapsamı dışında. Cancellation, şehir listesi sorgusunda örnekleniyor.

`LocalMailService` ve `CloudMailService`, e-posta davranışını konsola yazarak örnekler. Önceki dersten gelen dosya controller'ı, indirme isteğinde yerel `sample-file.pdf` dosyasını arar ve dosya yoksa 404 döndürür. Yükleme işlemi, istekte belirtilen content type değerini ve dosya boyutunu denetler. Örnek PDF'ler ve yüklenen dosyalar yerelde tutulur.

## Öğrenme süreci ve kaynak

Bellek içi veriden kalıcı EF Core sorgularına geçtim. Foreign key ile navigation property ilişkisini, entity ve DTO ayrımını ve repository üzerinden veri değiştirme işlemlerini uyguladım. Dependency injection, migration araçları, toplu güncelleme ifadeleri, cancellation ve eklenen filtrelerin son sorguya taşınması üzerinde çalıştım.

CityInfo örneği ve derslerin kurgusu Kevin Dockx'un yukarıda bağlantısı verilen Pluralsight eğitimine dayanıyor. Bu depoda eğitim sırasında yaptığım uygulama ve uyarlamalar yer alıyor. Bazı açıklamalarda, hata gidermede, kod düzenlemelerinde ve dokümantasyonda yapay zekâ desteği kullandım. Üçüncü taraf paketlerin kendi lisansları geçerlidir.

