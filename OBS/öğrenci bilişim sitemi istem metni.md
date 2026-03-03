Harika, orijinal metninin ruhunu ve detaylarını eksiksiz bir şekilde koruyarak, yalnızca belirlediğimiz o eksik detayları ve numara düzenlemelerini metne entegre ettim. Hiçbir cümlen sadeleştirilmedi veya silinmedi.
İşte yazılımcıya doğrudan gönderebileceğin, eksiksiz ve numaralandırması kusursuz hale getirilmiş o mükemmel istem metni:
________________________________________
📘 ÖĞRENCİ BİLGİ SİSTEMİ (OBS)
________________________________________
1️⃣ PROJE GENEL TANIMI
Proje Adı:
Öğrenci Bilgi Sistemi (OBS – Desktop Uygulama)
Amaç:
Öğrencinin okul ile ilgili tüm bilgilerine (kimlik, sınıf, fotoğraf, veli bilgisi vb.) hızlı, sade ve modern bir masaüstü arayüz üzerinden erişim sağlamak.
Platform:
• Windows Desktop
• .NET WPF
• SQLite veritabanı
________________________________________
2️⃣ GENEL TEKNİK BEKLENTİLER
• Uygulama masaüstü (WPF) olacaktır.
• UI modern, sade, profesyonel.
• Kullanıcı manuel resize yapamaz. Ancak taşınabilir olmalı. Pencere genişliği yalnızca sistem tarafından kontrol edilir.
• Kod modüler ve katmanlı olacak:
o UI Layer
o Business Logic Layer
o Data Access Layer
• Açıklama satırları içermeli.
• Manuel müdahaleye açık mimari.
• Tüm veriler kalıcı saklanacak.
• Otomatik silme yapılmayacak (merge mantığı).
• Duplicate oluşmayacak.
• Veri şişmesi olmayacak.
• Tüm veri işlemleri transactional olacak.
• Kullanılacak 3. parti kütüphaneler (PDF okuma, içinden fotoğraf/embedded image çıkarma işlemleri için) lisans gerektirmeyen, tamamen ücretsiz ve açık kaynak (Open-Source) (örn. iText7, Pdfium vb.) olmalıdır. Excel'e aktarım işlemlerinde kullanıcının bilgisayarında Excel kurulu olma zorunluluğunu kaldıran Interop bağımsız kütüphaneler (örn. ClosedXML, EPPlus vb.) kullanılmalıdır.

PERFORMANS VE KAYNAK YÖNETİMİ KURALLARI:

Bellek (RAM) Optimizasyonu: PDF ayrıştırma ve fotoğraf işleme süreçlerinde kullanılan nesneler işlem biter bitmez bellekten tahliye edilmelidir (IDisposable yönetimi). Arayüzde (WPF) gösterilen öğrenci fotoğrafları, orijinal boyutunda değil, DecodePixelWidth özelliği kullanılarak sadece gösterim boyutunda (thumbnail) belleğe yüklenmelidir.

Disk ve Dosya Temizliği: Kullanıcı bir öğrenci kaydını manuel olarak sildiğinde, bu öğrenciye ait fiziksel fotoğraf dosyası da (.jpg) diskten kalıcı olarak silinmelidir. İşlem sırasında oluşan geçici (temp) dosyalar işlem sonunda sistemden temizlenmelidir.

Log Yönetimi: Log dosyası boyutu kontrol altında tutulmalı, aşırı şişme durumunda eski kayıtlar üzerine yazılacak şekilde (rolling log) yapılandırılmalıdır.






PDF VERİ TUTARLILIĞI VE FORMAT GARANTİSİ

PDF veri kaynakları ile ilgili aşağıdaki teknik kabuller geçerlidir:

• Kullanılacak tüm PDF dokümanlarının formatı sabittir ve değişkenlik göstermemektedir.
• PDF içeriğindeki metinler selectable (metin olarak okunabilir) yapıdadır.
• PDF dosyalarında font encoding problemi bulunmamaktadır. Metinler standart encoding ile düzgün şekilde ayrıştırılabilmektedir.
• Öğrenci fotoğrafları PDF içerisinde embedded image object olarak yer almaktadır ve extraction işlemi teknik olarak mümkündür.
• Bu yapı daha önce HTML tabanlı bir sistem içerisinde başarıyla test edilmiştir.
• StudentNumber alanı tüm PDF kaynaklarında tutarlıdır, formatı standarttır ve veri bütünlüğü açısından güvenilirdir.

Dolayısıyla veri ayrıştırma (extraction) sürecinde yapısal bir belirsizlik veya format kaynaklı teknik risk beklenmemektedir


FOREIGN KEY VE İLİŞKİSEL SİLME KURALLARI

SQLite üzerinde foreign key constraint’ler aktif edilecek ve uygulama başlatıldığında PRAGMA foreign_keys = ON komutu çalıştırılacaktır.

İlişkisel silme davranışları aşağıdaki şekilde tanımlanacaktır:

• Teams tablosundan bir kayıt silindiğinde, bu takıma bağlı tüm TeamMembers kayıtları otomatik olarak silinecektir (ON DELETE CASCADE).

• Students tablosundan bir kayıt silindiğinde, bu öğrenciye bağlı tüm TeamMembers kayıtları otomatik olarak silinecektir (ON DELETE CASCADE).

• Students tablosundan bir kayıt silindiğinde, ilgili Guardian kaydı da otomatik olarak silinecektir (ON DELETE CASCADE).

Hiçbir durumda orphan (yetim) kayıt oluşmasına izin verilmeyecektir. Veri bütünlüğü ilişkisel düzeyde korunacaktır.



INDEX VE PERFORMANS STRATEJİSİ

Büyük veri hacimlerinde performansın korunması amacıyla aşağıdaki index’ler oluşturulacaktır:

• Students tablosunda StudentNumber alanı UNIQUE index’e sahip olacaktır. Merge ve arama performansı için zorunludur.

• Students tablosunda Class alanı için non-unique index oluşturulacaktır. Sınıf filtreleme ve listeleme işlemlerinin performansını artırmak amacıyla kullanılacaktır.

• TeamMembers tablosunda TeamId alanı için index oluşturulacaktır. Takım bazlı öğrenci listeleme işlemlerinin hızlı çalışması sağlanacaktır.

• TeamMembers tablosunda (TeamId + StudentId) alanları üzerinde UNIQUE composite index oluşturulacaktır. Aynı öğrencinin aynı takıma birden fazla eklenmesi veritabanı seviyesinde engellenecek ve ilişki kontrolleri performanslı şekilde yürütülecektir.

Index tanımları büyük veri hacimlerinde full table scan oluşmasını engellemek ve UI performansını korumak amacıyla zorunlu tutulmaktadır.
VERİ KAYNAĞI SOYUTLAMA VE GELECEK GENİŞLEYEBİLİRLİK

Sistem yalnızca PDF veri kaynağına bağımlı olacak şekilde tasarlanmayacaktır. Veri içe aktarma süreci soyutlanmış bir servis katmanı üzerinden yürütülecektir.

MergeService doğrudan PDF, Excel veya başka bir dosya tipini bilmeyecek; yalnızca standartlaştırılmış bir veri modeli (örneğin StudentImportModel) üzerinden çalışacaktır.

Bu amaçla veri içe aktarma işlemleri arayüz (interface) tabanlı bir yapı üzerinden gerçekleştirilecektir (örneğin IDataImportService).

PDF import işlemi PdfImportService üzerinden yürütülecek olup, ileride Excel, CSV veya API tabanlı veri kaynakları sisteme eklenmek istendiğinde mevcut Merge ve UI katmanları değiştirilmeden yalnızca yeni bir import servisi eklenerek sistem genişletilebilecektir

________________________________________
3️⃣ UYGULAMA MİMARİSİ
Uygulama 3 ana bölümden oluşur:
1.	Ana Panel (Öğrenci Arama)
2.	Bilgi Giriş Paneli
3.	Okul Takımı Yönetim Paneli
________________________________________
4️⃣ GİRİŞ GÜVENLİĞİ (PIN SİSTEMİ)
Uygulama açılışında:
• 4 haneli PIN ekranı
• Basit doğrulama
• PIN hash’lenerek DB’de saklanabilir
• Ayarlar bölümünden değiştirilebilir
Uygulamanın ilk defa açıldığı durumda (henüz bir PIN yokken) kullanıcıdan doğrudan yeni bir PIN oluşturması istenir.
PIN unutulması durumunda uygulama klasörüne özel “admin.reset” dosyası eklenecek.
Uygulama açıldığında PIN doğrulaması bypass edilir ve yeni PIN oluşturma ekranı açılır.
Reset işlemi tamamlandıktan sonra dosya otomatik silinir.
________________________________________
5️⃣ ANA PANEL (ÖĞRENCİ ARAMA)
Tasarım Özellikleri
• Telefon görünümünde sabit panel
• Kullanıcı pencereyi manuel olarak boyutlandıramaz (resize yapamaz), ancak pencere taşınabilir olmalıdır.
• Uygulama "Her Zaman Üstte" (Topmost) çalışma mantığına sahip olmalıdır. Böylece arkada web tarayıcısı (lisans çıkarma sayfaları vb.) açıkken kopyala-yapıştır işlemleri için ekranda sabit bir asistan gibi kalmalıdır.
• Çözünürlük ve ekrana sığmama risklerini sıfırlamak için pencere genişliği sistem tarafından, "Katlanabilir Yan Menü (Collapsible Sidebar)" mimarisi ile yönetilecektir.
• Sadece simge ile küçülür
• Üstte:
o Logo (dinamik değiştirilebilir)
o “ÖĞRENCİ BİLGİ SİSTEMİ” başlığı
________________________________________
5.1 Arama Çubuğu
Placeholder:
“Öğrenci ara: Ad, Soyad, No”
Özellikler:
• Live search
• Partial match
• Case insensitive
• Ad, Soyad, StudentNumber ile arama
________________________________________
5.2 Öğrenci Kart Tasarımı
Kart içeriği:
Sol:
• Fotoğraf
• Sağ tık → PNG export (Ad_Soyad_Numara.png)
Sağ:
• Ad Soyad (Başlık)
• Sınıf
• Sınıf No
• TC No
• Doğum Tarihi
• Veli No
Sağ üst:
• Favori butonu
Sağ alt:
• Künye butonu
________________________________________
6️⃣ FAVORİ SİSTEMİ
• Kalıcı saklanır
• Uygulama kapansa da kaybolmaz
• “Favorileri Göster” filtresi vardır
• Toplu silme yapılabilir
________________________________________
7️⃣ SINIF SEÇİM ALANI
Başlık:
“Sınıf Seçiniz”
Dropdown entegre açılır.
Seçildiğinde 3 buton görünür:
1.	Sınıf Listesi (Printable)
2.	Fotoğraf Listesi (Printable)
3.	Excel’e Aktar
Excel içeriği:
• Ad Soyad
• Sınıf
• Sınıf No
________________________________________
8️⃣ BİLGİ GİRİŞ PANELİ
Alt animasyonla açılır.
Üstte:
• Toplam Öğrenci Sayısı
• Toplam Sınıf Sayısı
________________________________________
8.1 Veri Yükleme Butonları
• Künye PDF Yükle
• Sınıf PDF Yükle
• Fotoğraf Listesi Yükle
• Veli No Yükle
Tüm yüklemeler otomatik MERGE mantığında çalışacaktır.
________________________________________
9️⃣ VERİ BİRLEŞTİRME (MERGE) VE GÜNCELLEME KURALLARI
Merge işlemi StudentNumber alanı üzerinden yürütülecektir. StudentNumber alanı veritabanında UNIQUE olacaktır ve her öğrenci için tekil kimlik görevi görecektir.
Merge sürecinde yalnızca INSERT ve UPDATE işlemleri yapılacaktır. Veritabanında mevcut olup yeni yüklenen PDF içerisinde bulunmayan kayıtlar otomatik olarak silinmeyecektir. Otomatik DELETE işlemi yapılması kesinlikle yasaktır. Silme işlemi yalnızca manuel kullanıcı aksiyonu ile gerçekleştirilebilir.
Tüm merge işlemleri transactional olarak yürütülecektir. İşlem sırasında hata oluşması durumunda veri bütünlüğü korunmalı ve işlem rollback edilmelidir.
Merge öncesinde StudentNumber alanı normalize edilecektir. Başındaki ve sonundaki boşluk karakterleri temizlenecek, gereksiz whitespace karakterleri kaldırılacaktır. Format tutarsızlığı nedeniyle duplicate oluşmasına izin verilmeyecektir.
SQLite üzerinde UPSERT mekanizması kullanılacaktır. Kullanılacak yapı ON CONFLICT(StudentNumber) DO UPDATE formatında olacaktır. INSERT OR REPLACE komutu kullanılmayacaktır. Çünkü REPLACE komutu önce DELETE sonra INSERT çalıştırdığı için foreign key ilişkilerini bozma riski taşır ve bu kabul edilemez.
Öğrenci güncelleme mantığı aşağıdaki şekilde olacaktır:
• StudentNumber veritabanında yoksa → INSERT
• StudentNumber varsa → UPDATE
Fotoğraf mevcutsa overwrite edilir. Aynı PDF tekrar yüklendiğinde yeni kayıt oluşturulmaz, veri şişmesi meydana gelmez ve yalnızca UPDATE işlemi çalışır.
Sınıf güncelleme sürecinde StudentNumber mevcutsa Class alanı güncellenir. StudentNumber veritabanında yoksa kayıt eklenmez ve durum log’a yazılır (Log kayıtları uygulamanın kurulu olduğu dizinde veya AppData içinde oluşturulacak 'Logs/app_log.txt' tarzı bir dosyada fiziksel olarak tutulacaktır). PDF’de bulunmayan öğrenciler silinmez.
Merge işlemi hiçbir koşulda toplu silme, otomatik silme veya pasif kayıt temizleme işlemi gerçekleştirmeyecektir. Veri kaybına sebep olabilecek davranışlara izin verilmeyecektir.
________________________________________
🔟 VERİTABANI MİMARİSİ
Kullanılacak DB:
SQLite (SQLite veritabanı dosyası (.db) ve fotoğrafların saklanacağı klasör, Windows yönetici izni (admin rights) kısıtlamalarına takılmamak adına Windows'un AppData\Local\OBS_System dizininde saklanmalıdır.)
Tablolar:
• Students
• Classes
• Guardians
• Favorites
• Teams
• TeamMembers
• Settings (PIN için)
Fotoğraflar:
• Dosya sisteminde saklanır
• DB’de sadece path tutulur
Foto isim kuralı:
{Ad_soyad_numara}.jpg (Dosya isimlendirmesinde Türkçe karakterler İngilizceye çevrilmeli -slugify- ve boşluklar alt tire ile değiştirilmelidir. Örn: ç->c, ş->s, ğ->g, boşluk->_).
Kayıp Fotoğraflar: Künye PDF yüklendiğinde içinden bir şekilde fotoğraf çıkmazsa (veya formatı bozuksa), sistemin varsayılan (default) bir avatar (örneğin boş bir silüet) ataması iyi bir fallback (güvenlik) senaryosu olur.
________________________________________
1️⃣1️⃣ SİSTEMİ SIFIRLA
Ayarlar (dişli ikon) içinde yer alır.
Çift onay ister.
Yapılacaklar:
• Tüm DB silinir
• Fotoğraf klasörü temizlenir
• Favoriler dahil her şey silinir
• Sistem sıfırlama işleminde öğrenci, takım, favori ve fotoğraf verileri silinir.
Settings tablosundaki PIN verisi korunur.
________________________________________
1️⃣2️⃣ OKUL TAKIMI YÖNETİM PANELİ
________________________________________
12.1 Arayüz Yapısı
• Takım yönetim ekranı ana panel ile aynı tasarım dilini kullanacaktır.
• Uygulama, çözünürlük sorunlarını önlemek amacıyla "Katlanabilir Yan Menü (Collapsible Sidebar)" mimarisini kullanacaktır.
• Ekran iki sütunlu (split layout) yapı kullanacak ancak toplam pencere genişliği kontrol altında tutulacaktır.
• Sol sütun (Daraltılmış Ana Panel): Takım paneli açıkken, ana panel ince dikey bir şerit formunu alacaktır.
• Sağ sütun (Takım Paneli): Takım yönetim paneli, kullanıcının rahat çalışabilmesi için geniş bir alan (örn. 600px) kaplayacaktır.
• Her iki panel (kendi aktif durumlarında) tamamen bağımsız scroll (kaydırma) alanlarına sahip olacaktır.
________________________________________
12.2 Panel Açılma ve Kapanma Davranışı
“Takımlar” butonuna basıldığında uygulama penceresi kontrolsüz bir şekilde sağa doğru genişlemeyecek, bunun yerine akıllı alan yönetimi devreye girecektir:
• Daralma Animasyonu: Ana panel, yumuşak bir animasyonla (200–300 ms) sola doğru daralarak şık ve dikey bir şeride (örn. 50px) dönüşecektir.
• Şerit Tasarımı: Bu daralan şeridin üzerinde, kullanıcıyı yönlendirmek için vurgulu bir renkte "Ana Ekrana Dön" butonu/ikonu yer alacaktır.
• Açılma Animasyonu: Ana panel daralırken, sağ panel (Takım Yönetimi) eşzamanlı olarak 0'dan hedef genişliğe (min. 600px) kayarak açılacaktır.
• Kapanma (Geri Dönüş): Kullanıcı sol taraftaki ince şeride (Ana Ekrana Dön) tıkladığında, takım paneli animasyonlu olarak kapanacak (genişlik 0'a düşecek) ve ana panel tekrar eski "Asistan Modu" formuna (örn. 400px) genişleyecektir.
• Panel geçişleri sırasında herhangi bir veri kaybı veya silinme işlemi kesinlikle yaşanmayacaktır.
________________________________________
12.3 Panel İç Yapısı
Takım paneli aşağıdaki bölümlerden oluşacaktır:
1.	Takım başlık alanı
2.	Oyuncu ekleme alanı (arama + filtre)
3.	Accordion yapısında takım listesi
4.	Seçilen takımın öğrenci kartları
________________________________________
12.4 Accordion Yapısı ve Davranışı
• Takımlar başlık bazlı accordion (katlanabilir) yapıda listelenecektir.
• Varsayılan açılışta tüm takımlar kapalı (collapsed) durumda olacaktır.
• Bir takım başlığına tıklandığında ilgili öğrenci kartları animasyonlu şekilde aşağı doğru açılacaktır (slide down).
• Açılma/kapanma işlemleri animasyonlu olacaktır.
• Aynı anda yalnızca bir takım açık olacaktır.
• Yeni bir takım açıldığında daha önce açık olan takım otomatik olarak kapanacaktır.
• Öğrenci kartları sayfa ilk yüklendiğinde değil, ilgili takım açıldığında yüklenecektir (lazy loading).
________________________________________
12.5 Takım Kartı (Başlık Alanı)
Her takım aşağıdaki bilgileri içeren bir başlık kartı olarak gösterilecektir:
• TeamName (kalın yazı)
• MatchDate (küçük metin)
• Oyuncu Sayısı (badge formatında)
• Aç/Kapat butonu
• Düzenle butonu
• Sil butonu
Başlık solunda takım durumunu gösteren renk etiketi bulunabilir (opsiyonel).
________________________________________
12.6 Takım Oluşturma Davranışı
• “Yeni Takım Oluştur” butonu bulunacaktır.
• Butona tıklandığında modal pencere açılacaktır.
Giriş Alanları:
• TeamName (Zorunlu)
• MatchDate (Opsiyonel)
• Description (Opsiyonel)
Kurallar:
• TeamName UNIQUE olacaktır.
• Aynı isimle iki takım oluşturulamaz.
• Boş isimle kayıt yapılamaz.
• Başarılı işlem sonrası toast mesaj gösterilecektir.
________________________________________
12.7 Takıma Öğrenci Ekleme
• Takım paneli içerisinde arama çubuğu bulunacaktır.
• Öğrenci kartı tasarımı ana panel tasarımı ile aynı olacaktır.
• Favori butonu bulunmayacaktır.
• “Takıma Ekle” butonu yer alacaktır.
Kurallar:
• Aynı öğrenci aynı takıma iki kez eklenemez.
• DB seviyesinde UNIQUE (TeamId + StudentId) constraint uygulanacaktır.
• Takıma ekleme işlemi TeamMembers ilişki tablosu üzerinden gerçekleştirilecektir.
• Öğrenci verileri Students tablosundan referans alınacaktır.
• Veri kopyalama yapılmayacaktır.
• Başarılı eklemede toast mesaj gösterilecektir.
________________________________________
12.8 Takım İçindeki Öğrenci Kartı İçeriği
Takım açıldığında öğrenci kartları aşağı doğru listelenecektir.
Kartlar mini tasarım olmayacak, bilgi erişim odaklı olacaktır.
Kart içeriği:
• Foto
• Ad Soyad
• Sınıf
• TC No
• Doğum Tarihi (küçük font)
• “Detay” butonu
________________________________________
12.9 Takımdan Öğrenci Çıkarma
• Her öğrenci kartında “Takımdan Çıkar” butonu bulunacaktır.
• İşlem sonrası liste anlık olarak güncellenecektir.
• Başarılı işlem sonrası toast mesaj gösterilecektir.
________________________________________
12.10 Takım Silme Davranışı
• Silme işlemi onay popup gerektirir.
• Teams tablosundaki kayıt silinir.
• İlişkili TeamMembers kayıtları silinir.
• Students tablosu etkilenmez.
• İşlem transactional olacaktır.
________________________________________
12.11 Veri Bağlantısı
• Takım paneli Students tablosundaki verileri referans alacaktır.
• Takıma ekleme ve çıkarma işlemleri TeamMembers tablosu üzerinden yürütülecektir.
• Veri kopyalama yapılmayacaktır.
________________________________________
12.12 Kalıcılık
• Takımlar kalıcıdır.
• TeamMembers kayıtları kalıcıdır.
• Uygulama kapansa dahi veri korunur.
________________________________________
12.13 Performans Kuralları
• Öğrenci kartları ilgili takım açıldığında yüklenecektir (lazy loading).
• UI donmaması için async veri çağrısı yapılmalıdır.
• Çok sayıda takım olması durumunda arayüz karışmamalıdır.
• Veri işlemleri UI thread’i bloklamayacak şekilde yürütülmelidir.
________________________________________
1️⃣3️⃣ TOAST MESAJ SİSTEMİ
Büyük popup kullanılmayacak.
Sağ alt veya üstte:
• Yeşil: Başarılı
• Kırmızı: Hata
3 saniye görünür.
________________________________________
1️⃣4️⃣ VERİ KAYNAKLARI
A) KÜNYE PDF
Özellikler:
• Çok sayfalı
• Her sayfa 1 öğrenci
• Format sabit
• Metin selectable
• Foto embedded image object
Çekilecek Alanlar:
• StudentNumber
• Ad Soyad
• TC No
• Doğum Tarihi
• Fotoğraf
Foto:
• En büyük image alınır
• Overwrite yapılır
________________________________________
B) SINIF LİSTESİ PDF
• Tek PDF
• Tüm sınıflar mevcut
• Başlık formatı sabit
Çekilecek:
• StudentNumber
• Class
________________________________________
1️⃣5️⃣ KOD MİMARİSİ VE DOSYA/KLASÖR YAPISI (MODÜLER SİSTEM)
Genel Beklenti: Proje kodları kesinlikle tek bir dosyaya (monolithic yapı) yığılmayacaktır. Tüm sistem; fiziksel olarak ayrılmış, birbirinden bağımsız, modüler .cs ve .xaml dosyalarından oluşacaktır. Temel prensip "Görevler Ayrılığı" (Single Responsibility) olacaktır.
Klasör ve Dosya Hiyerarşisi: Proje ağacı aşağıdaki gibi fiziksel klasörlere ve mantıksal katmanlara ayrılacaktır:
• 📁 Views (Arayüz Katmanı): Sadece kullanıcı arayüzü dosyalarını (.xaml) barındırır. XAML dosyalarının arka planındaki kodlarda (Code-behind) kesinlikle veritabanı bağlantısı veya iş mantığı (business logic) bulunmayacaktır. (Örn: MainWindow.xaml, TeamPanelView.xaml)
• 📁 ViewModels (Arayüz Kontrol Katmanı): Kullanıcı etkileşimlerini (buton tıklamaları, arama kutusu tetiklemeleri) dinleyen ve ilgili servislere yönlendiren dosyalar. (Örn: MainViewModel.cs)
• 📁 Services (İş Mantığı Katmanı): Uygulamanın ana operasyonlarının yürütüldüğü özel dosyalardır. Her büyük işlem kendi fiziksel dosyasına sahip olmalıdır:
• MergeService.cs: Sadece verilerin güncellenmesi, UPSERT, Transaction/Rollback işlemleri ve veri normalizasyonu.
• PdfExtractionService.cs: Sadece PDF okuma ve metin/fotoğraf ayrıştırma işlemleri.
• 📁 DataAccess (Veritabanı Katmanı): Yalnızca SQLite veritabanı ile iletişim kuran bağlantı ve sorgu dosyaları burada yer alacaktır. İş kuralları bu katmana sızdırılmayacaktır. (Örn: DatabaseConnection.cs, TeamRepository.cs)
• 📁 Models (Varlıklar): Veritabanı tablolarını temsil eden saf C# sınıfları. (Örn: Student.cs, Team.cs)
Kodlama Standartları ve Kurallar:
• Zorunlu Açıklama Satırları: Yazılan her sınıfın (class), metodun ve özellikle karmaşık algoritmaların (örneğin Merge mantığı) üzerinde, o kodun ne işe yaradığını anlatan detaylı ve anlaşılır açıklama satırları (comments) bulunması zorunludur.
• İzolasyon: Veritabanına öğrenci ekleme kodu ile arayüzdeki bir animasyon kodu kesinlikle aynı dosyada bulunmayacaktır.
• Geliştirme Yaklaşımı: Geliştirici, modülleri kodlamaya başlamadan önce veya kritik entegrasyon adımlarında mutlaka onay alarak (mutabık kalarak) ilerleyecektir. Sistem, ileride "Okul Takımı Yönetim Paneli" gibi yeni bir modül eklendiğinde mevcut kodları bozmadan sadece yeni klasör/dosya ekleyerek genişleyebilecek bir esneklikte olmalıdır.

