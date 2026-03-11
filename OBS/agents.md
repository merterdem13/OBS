# AGENTS.md

## Must-follow constraints

* **UI Framework:** Projede standart WPF kontrolleri yerine **WPF UI (v4.2.0 - lepo.co)** kullanılmaktadır. Arayüz elemanları için XAML'da her zaman `xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"` ad alanını (namespace) kullanın (Örn: `<Button>` yerine `<ui:Button>`).
* **Pencereler (Windows):** Yeni pencereler kesinlikle standart `Window` yerine `<ui:FluentWindow>` sınıfından türetilmelidir.
* **MVVM & State Management:** Projede `CommunityToolkit.Mvvm` paketleri aktiftir. `INotifyPropertyChanged` veya `ICommand` arayüzlerini manuel olarak uygulamayın.
* Sınıfları `ObservableObject` sınıfından türetin.
* Sadece *private* field'lar tanımlayıp `[ObservableProperty]` niteliğini (attribute) ekleyin (Field'lar `camelCase` formatında olmalıdır, oluşturulan property `PascalCase` olacaktır).
* İlgili aksiyon metotlarına `[RelayCommand]` ekleyin. (Örn: `async Task SaveAsync()` metodu XAML'da bind edilmek üzere otomatik `SaveCommand` üretir).


* **Code-Behind Kuralları:** `Views/*.xaml.cs` dosyalarında (code-behind) kesinlikle iş mantığı (business logic) veya veritabanı sorgusu yazmayın. Yalnızca animasyonlar veya arayüze has UI tepkileri burada olabilir. Veri akışı daima ViewModels üzerinden `Binding` ile yapılmalıdır.

## Repo-specific conventions

* **Temalandırma & Renkler:** XAML dosyalarında statik renk kodları (Örn: `#FFFFFF` veya `White`) kullanmaktan kesinlikle kaçının. Dark/Light mode sisteminin bozulmaması için daima Wpf.Ui'nin tema kaynaklarını (`DynamicResource TextFillColorPrimaryBrush` vb.) kullanın.
* **Veritabanı Şifrelemesi (Gotcha):** Uygulamadaki SQLite veritabanı **SQLCipher** ile şifrelenmiştir (`OBS/DataAccess/DatabaseConnection.cs`). Arayüz için test/mock verisi çekerken veya debug yaparken standart SQLite araçlarıyla veritabanına bağlanmaya çalışmayın, bu dosyayı bozabilir veya hata fırlatır. Tüm işlemler için mevcut Repository (örn: `StudentRepository`) sınıflarını kullanın.

## Important locations

* `OBS/Views/` & `OBS/Views/Components/`: XAML pencereleri, Sayfalar (Pages) ve tekrar kullanılabilir UI bileşenleri (UserControls).
* `OBS/ViewModels/`: View'ların bağlandığı (DataContext) iş mantığı ve durum (state) sınıfları.
* `OBS/Converters/`: XAML içerisinde verileri formata uygun göstermek için (örn. BoolToVisibility) yazılmış `IValueConverter` araçları.
* `OBS/App.xaml`: Uygulama genelinde paylaşılan global stiller ve `Wpf.Ui` tema sözlükleri (Dictionaries).

## Change safety rules

* Mevcut `Wpf.Ui` kontrollerinin varsayılan stillerini (Style) ezerken, genel Fluent UI tasarım dilini (yuvarlatılmış köşeler, vurgu renkleri vb.) bozmadığınızdan emin olun.
* Asenkron işlemleri çalıştıracak buton komutlarında arayüzün kilitlenmemesi (UI freeze) için mutlaka metodu `async Task` olarak tanımlayıp `[RelayCommand]` atamasını bu metoda yapın.