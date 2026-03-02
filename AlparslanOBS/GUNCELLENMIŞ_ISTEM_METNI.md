# 📘 ÖĞRENCİ BİLGİ SİSTEMİ (OBS) - GÜNCELLENMIŞ VERSIYONU

**SON GÜNCELLEME:** Arayüz tasarım detayları ve modüler mimari gereksinimleri eklendi

________________________________________
## 1️⃣ PROJE GENEL TANIMI

### Proje Adı:
Öğrenci Bilgi Sistemi (OBS – Desktop Uygulama)

### Amaç:
Öğrencinin okul ile ilgili tüm bilgilerine (kimlik, sınıf, fotoğraf, veli bilgisi vb.) hızlı, sade ve modern bir masaüstü arayüz üzerinden erişim sağlamak.

### Platform:
• Windows Desktop  
• .NET WPF  
• SQLite veritabanı

### Arayüz Standartları:
• **Standart Pencere Boyutu:** 400x750 piksel (Telefon görünümü)
• **Genişleme Modu:** Katlanabilir yan menü ile maksimum 1000px'e kadar
• Modern, pratik, minimalist tasarım
• **Tasarım Prensibi:** Her panel/bölüm AYRI bir modül/component olacak

________________________________________
## 2️⃣ GENEL TEKNİK BEKLENTİLER

### Temel Kurallar:
• Uygulama masaüstü (WPF) olacaktır.
• UI modern, sade, profesyonel.
• **Kullanıcı manuel resize yapamaz.** Ancak taşınabilir olmalı. 
• Pencere genişliği yalnızca sistem tarafından kontrol edilir.

### 🔴 MODÜLER MİMARİ ZORUNLULUĞU:
Tüm arayüz bileşenleri ayrı, bağımsız modüller/components olarak geliştirilecek:
1. ✅ **Header Modülü** (Logo, başlık, minimize/kapat butonları)
2. ✅ **Ana Panel Modülü** (Öğrenci arama ve listeleme)
3. ✅ **Student Card Component** (Öğrenci kartı)
4. ✅ **Favoriler Paneli Modülü** (Sol alt, fade animasyonlu)
5. ✅ **Sınıf Seçim Modülü** (Dropdown + işlem butonları)
6. ✅ **Bilgi Giriş Paneli Modülü** (PDF yükleme)
7. ✅ **Yönetim Paneli Butonu Modülü** (Sağ alt, blurlu)
8. ✅ **Takım Yönetim Paneli Modülü** (Genişleyebilir sağ panel)
9. ✅ **Login Ekranı Modülü** (PIN girişi)

### Kod Katmanları:
• **UI Layer** → Sadece görsel bileşenler
• **Business Logic Layer** → İş kuralları
• **Data Access Layer** → Veritabanı işlemleri

### Diğer Teknik Gereksinimler:
• Açıklama satırları içermeli.
• Manuel müdahaleye açık mimari.
• Tüm veriler kalıcı saklanacak.
• Otomatik silme yapılmayacak (merge mantığı).
• Duplicate oluşmayacak.
• Veri şişmesi olmayacak.
• Tüm veri işlemleri transactional olacak.
• **3. Parti Kütüphaneler:** Lisans gerektirmeyen, tamamen ücretsiz ve açık kaynak (örn. iText7, Pdfium, ClosedXML, EPPlus vb.)

### PERFORMANS VE KAYNAK YÖNETİMİ KURALLARI:

**Bellek (RAM) Optimizasyonu:**
• PDF ayrıştırma ve fotoğraf işleme süreçlerinde kullanılan nesneler işlem biter bitmez bellekten tahliye edilmeli (IDisposable yönetimi)
• Arayüzde (WPF) gösterilen öğrenci fotoğrafları, DecodePixelWidth özelliği kullanılarak sadece gösterim boyutunda (thumbnail) belleğe yüklenmelidir.

**Disk ve Dosya Temizliği:**
• Öğrenci kaydı silindiğinde, fiziksel fotoğraf dosyası da (.jpg) diskten kalıcı olarak silinmeli
• İşlem sırasında oluşan geçici (temp) dosyalar işlem sonunda temizlenmeli

**Log Yönetimi:**
• Log dosyası boyutu kontrol altında tutulmalı (rolling log)

### PDF VERİ TUTARLILIĞI VE FORMAT GARANTİSİ

PDF veri kaynakları ile ilgili aşağıdaki teknik kabuller geçerlidir:
• Kullanılacak tüm PDF dokümanlarının formatı sabittir
• PDF içeriğindeki metinler selectable (metin olarak okunabilir) yapıdadır
• Font encoding problemi bulunmamaktadır
• Öğrenci fotoğrafları PDF içerisinde embedded image object olarak yer almaktadır
• StudentNumber alanı tüm PDF kaynaklarında tutarlıdır

### FOREIGN KEY VE İLİŞKİSEL SİLME KURALLARI

SQLite üzerinde foreign key constraint'ler aktif edilecek:
```sql
PRAGMA foreign_keys = ON
```

İlişkisel silme davranışları:
• Teams → TeamMembers (ON DELETE CASCADE)
• Students → TeamMembers (ON DELETE CASCADE)
• Students → Guardian (ON DELETE CASCADE)

### INDEX VE PERFORMANS STRATEJİSİ

• Students.StudentNumber → UNIQUE index (merge ve arama için zorunlu)
• Students.Class → Non-unique index (sınıf filtreleme için)
• TeamMembers.TeamId → Index (takım listeleme için)
• TeamMembers (TeamId + StudentId) → UNIQUE composite index

### VERİ KAYNAĞI SOYUTLAMA VE GENİŞLEYEBİLİRLİK

• MergeService doğrudan dosya tipini bilmeyecek
• Standartlaştırılmış veri modeli kullanılacak (StudentImportModel)
• Interface tabanlı yapı (IDataImportService)
• PDF import → PdfImportService
• İleride Excel, CSV, API desteği eklenebilecek

________________________________________
## 3️⃣ UYGULAMA MİMARİSİ

Uygulama ana modüllerden oluşur:
1. **Login Ekranı** (PIN doğrulama)
2. **Ana Panel** (Öğrenci arama ve listeleme)
3. **Bilgi Giriş Paneli** (Veri yükleme)
4. **Okul Takımı Yönetim Paneli** (Takım oluşturma ve yönetimi)

________________________________________
## 4️⃣ GİRİŞ GÜVENLİĞİ (PIN SİSTEMİ) - Modül

### Özellikler:
• 4 haneli PIN ekranı
• Basit doğrulama
• PIN hash'lenerek DB'de saklanır
• Ayarlar bölümünden değiştirilebilir

### 🆕 GİRİŞ YÖNTEMLERİ:
• **Numpad (Ekrandaki sayısal tuş takımı)** → Dokunmatik deneyim için
• **Klavyenin üst sırasındaki sayı tuşları (1-9, 0)**
• **Enter tuşu** ile doğrulama

### İlk Açılış:
Uygulamanın ilk defa açıldığı durumda (henüz bir PIN yokken) kullanıcıdan doğrudan yeni bir PIN oluşturması istenir.

### PIN Sıfırlama:
• Uygulama klasörüne "admin.reset" dosyası eklenecek
• Uygulama açıldığında PIN doğrulaması bypass edilir
• Yeni PIN oluşturma ekranı açılır
• Reset işlemi sonunda dosya otomatik silinir

________________________________________
## 5️⃣ ANA PANEL (ÖĞRENCİ ARAMA) - Modül

### Tasarım Özellikleri:
• **Boyut:** 400x750 piksel (sabit, telefon görünümü)
• **Resize:** Kullanıcı manuel boyutlandıramaz
• **Taşınabilir:** Evet
• **Topmost:** Her zaman üstte kalır (arkada tarayıcı açıkken kopyala-yapıştır için)
• **Genişleme:** "Katlanabilir Yan Menü (Collapsible Sidebar)" mimarisi
• **Minimize:** Sadece simge ile küçülür

### 🆕 HEADER (Ayrı Modül):
• **Logo:** Dinamik değiştirilebilir
• **Başlık:** "ÖĞRENCİ BİLGİ SİSTEMİ"
• **Butonlar:** Minimize ve Kapatma
• **Tasarım:** Minimalist, ekranda çok yer kaplamayacak

### 5.1 Arama Çubuğu
**Placeholder:** "Öğrenci ara: Ad, Soyad, No"

**Özellikler:**
• Live search
• Partial match
• Case insensitive
• Ad, Soyad, StudentNumber ile arama

### 5.2 Öğrenci Kart Tasarımı (Student Card Component)

**🆕 KART LAYOUT:**

```
┌─────────────────────────────────────┐
│ ┌────┐  Ad Soyad (Kalın)        ⭐  │
│ │FOTO│  SINIF: 9-A                  │
│ │    │  NO: 123                     │
│ └────┘  TC: 12345678901             │
│         Doğum Tarihi: 01.01.2005    │
│         Veli No: 0555 123 45 67     │
│                           [KÜNYE]   │
└─────────────────────────────────────┘
```

**Sol Bölüm:**
• Kare biçimde öğrenci fotoğrafı
• Sağ tık → PNG export (Ad_Soyad_Numara.png)

**Sağ Bölüm (Üstten Alta):**
• **Ad Soyad** (Başlık, kalın yazı)
• SINIF: [değer]
• NO: [değer]
• TC: [değer]
• Doğum Tarihi: [değer]
• Veli No: [değer]

**Sağ Üst:**
• Favori butonu (⭐ yıldız ikonu)

**Sağ Alt:**
• Künye butonu (kırmızı renk)

________________________________________
## 6️⃣ FAVORİ SİSTEMİ - Modül

### Özellikler:
• Kalıcı saklanır
• Uygulama kapansa da kaybolmaz

### 🆕 FAVORİLER PANELİ (Ayrı Modül):

**Pozisyon:** Sol alt köşe

**Görünüm:**
• Fade in/fade out animasyonu
• Parıldayan, sırıtmayan görsel efekt

**Tetikleme:**
• Sadece bir öğrenci favoriye alınırsa pop-up gibi çıkar
• Favoride öğrenci yokken panel görünmez

**Davranış:**
• Basıldığında → Favori öğrenciler listelenir
• Tekrar basıldığında → Kapanır

**İşlevler:**
• "Favorileri Göster" filtresi
• Toplu silme özelliği

________________________________________
## 7️⃣ SINIF SEÇİM ALANI - Modül

### Başlık:
"Sınıf Seçiniz"

### Dropdown:
Entegre açılır menü

### Seçim Sonrası 3 Buton Görünür:
1. **Sınıf Listesi** (Printable)
2. **Fotoğraf Listesi** (Printable)
3. **Excel'e Aktar**

### Excel İçeriği:
• Ad Soyad
• Sınıf
• Sınıf No

________________________________________
## 8️⃣ BİLGİ GİRİŞ PANELİ - Modül

### Açılış:
Alt animasyonla açılır

### Üstte İstatistikler:
• Toplam Öğrenci Sayısı
• Toplam Sınıf Sayısı

### 8.1 Veri Yükleme Butonları:
• Künye PDF Yükle
• Sınıf PDF Yükle
• Fotoğraf Listesi Yükle
• Veli No Yükle

**Tüm yüklemeler otomatik MERGE mantığında çalışır.**

________________________________________
## 9️⃣ VERİ BİRLEŞTİRME (MERGE) VE GÜNCELLEME KURALLARI

### Merge İşlemi:
• StudentNumber alanı üzerinden yürütülür
• StudentNumber veritabanında UNIQUE olacak
• Yalnızca INSERT ve UPDATE yapılacak
• **Otomatik DELETE yasaktır**

### UPSERT Mekanizması:
```sql
ON CONFLICT(StudentNumber) DO UPDATE
```
❌ INSERT OR REPLACE kullanılmayacak (foreign key bozar)

### Merge Mantığı:
• StudentNumber yoksa → INSERT
• StudentNumber varsa → UPDATE
• Fotoğraf mevcutsa → Overwrite
• Aynı PDF tekrar yüklenirse → Sadece UPDATE (veri şişmesi olmaz)

### Sınıf Güncelleme:
• StudentNumber varsa → Class alanı güncellenir
• StudentNumber yoksa → Kayıt eklenmez, log yazılır
• PDF'de bulunmayan öğrenciler silinmez

### Normalizasyon:
• StudentNumber normalize edilecek (boşluklar temizlenir)
• Format tutarsızlığı nedeniyle duplicate oluşmasına izin verilmeyecek

### Transaction Yönetimi:
• Tüm merge işlemleri transactional
• Hata durumunda rollback

**🚫 UYARI:** Merge işlemi hiçbir koşulda toplu silme, otomatik silme veya pasif kayıt temizleme işlemi gerçekleştirmeyecek!

________________________________________
## 🔟 VERİTABANI MİMARİSİ

### Kullanılacak DB:
SQLite

### Dosya Konumu:
```
%AppData%\Local\OBS_System\
  ├── obs.db
  └── Photos\
      ├── Ali_Veli_12345.jpg
      ├── Ayse_Yilmaz_67890.jpg
      └── ...
```

### Tablolar:
• **Students** (Öğrenci bilgileri)
• **Classes** (Sınıf bilgileri)
• **Guardians** (Veli bilgileri)
• **Favorites** (Favori öğrenciler)
• **Teams** (Takımlar)
• **TeamMembers** (Takım üyeleri - ilişki tablosu)
• **Settings** (PIN ve ayarlar)

### Fotoğraflar:
• Dosya sisteminde saklanır
• DB'de sadece path tutulur
• **İsimlendirme:** `{Ad_soyad_numara}.jpg`
• **Slugify:** Türkçe karakterler İngilizceye (ç→c, ş→s, ğ→g)
• **Boşluk:** Alt tire (_) ile değiştirilir

### Kayıp Fotoğraflar:
Künye PDF'den fotoğraf çıkmazsa varsayılan avatar (boş silüet) atanır.

________________________________________
## 1️⃣1️⃣ SİSTEMİ SIFIRLA

### Konum:
Ayarlar (dişli ikon) içinde

### Güvenlik:
Çift onay ister

### İşlem:
• Tüm DB silinir
• Fotoğraf klasörü temizlenir
• Favoriler dahil her şey silinir
• **PIN verisi korunur**

________________________________________
## 1️⃣2️⃣ OKUL TAKIMI YÖNETİM PANELİ - Modül

### 🆕 12.1 YÖNETİM PANELİ BUTONU (Ayrı Modül)

**Pozisyon:** Sağ alt köşe

**Görünüm:**
• Blurlu (hafif bulanık) tasarım
• Çok göz almayacak, minimalist

**Hover Efekti:**
• Mouse üzerine geldiğinde blur kalkacak
• Hafif bump (şişme) efekti olacak

**İçerik (Basıldığında 2 Seçenek):**
1. **Ayarlar:**
   - Künye Yükle
   - Sistemi Sıfırla
2. **Takım Oluşturucu Paneli**

### 12.2 Arayüz Yapısı

• Takım yönetim ekranı ana panel ile aynı tasarım dilini kullanır
• "Katlanabilir Yan Menü (Collapsible Sidebar)" mimarisi
• İki sütunlu (split layout) yapı

**Genişleme Modu:**
```
┌────┐┌──────────────────────────────┐
│Ana ││ Takım Yönetim Paneli        │
│P   ││                              │
│a   ││ [Takım Listesi - Accordion] │
│n   ││                              │
│e   ││ [Öğrenci Arama]             │
│l   ││                              │
│    ││ [Seçilen Takım Üyeleri]     │
│    ││                              │
│[◄] ││                              │
└────┘└──────────────────────────────┘
 50px              600px
```

**Sol Sütun (Daraltılmış Ana Panel):**
• Takım paneli açıkken ince dikey şerit (örn. 50px)
• "Ana Ekrana Dön" butonu/ikonu (vurgulu renk)

**Sağ Sütun (Takım Paneli):**
• Geniş alan (örn. 600px)
• Rahat çalışma alanı

**Her İki Panel:**
• Bağımsız scroll alanları
• Panel geçişlerinde veri kaybı yok

### 12.3 Panel Açılma ve Kapanma Davranışı

**"Takımlar" Butonuna Basıldığında:**
1. Ana panel yumuşak animasyonla (200-300ms) sola daralardaralan şerit tasarımı olur
2. Sağ panel 0'dan hedef genişliğe kayarak açılır (eşzamanlı)

**Kapanma (Geri Dönüş):**
1. Sol şeritteki "Ana Ekrana Dön" butonuna tıklanır
2. Takım paneli animasyonlu kapanır (genişlik 0'a düşer)
3. Ana panel eski "Asistan Modu" formuna (400px) genişler

### 12.4 Panel İç Yapısı

Takım paneli bölümleri:
1. Takım başlık alanı
2. Oyuncu ekleme alanı (arama + filtre)
3. Accordion yapısında takım listesi
4. Seçilen takımın öğrenci kartları

### 12.5 Accordion Yapısı ve Davranışı

• Takımlar başlık bazlı accordion (katlanabilir) yapıda listelenir
• Varsayılan açılışta tüm takımlar kapalı (collapsed)
• Bir takım başlığına tıklandığında öğrenci kartları slide down ile açılır
• **Aynı anda yalnızca bir takım açık olacak**
• Yeni takım açılınca eskisi otomatik kapanır
• **Lazy loading:** Öğrenci kartları ilgili takım açıldığında yüklenir

### 12.6 Takım Kartı (Başlık Alanı)

Her takım başlığı:
• **TeamName** (kalın yazı)
• **MatchDate** (küçük metin)
• **Oyuncu Sayısı** (badge formatında)
• **Aç/Kapat** butonu
• **Düzenle** butonu
• **Sil** butonu
• **Renk Etiketi:** Takım durumunu gösteren (opsiyonel)

### 12.7 Takım Oluşturma Davranışı

**"Yeni Takım Oluştur" Butonu**
• Modal pencere açılır

**Giriş Alanları:**
• TeamName (Zorunlu)
• MatchDate (Opsiyonel)
• Description (Opsiyonel)

### 🆕 KATEGORİ SEÇİMİ (Zorunlu):
**Erkekler:**
- Futbol
- Futsal
- Basketbol
- Hentbol
- Voleybol
- Badminton

**Kadınlar:**
- Futbol (Kadınlar)
- Futsal (Kadınlar)
- Basketbol (Kadınlar)
- Hentbol (Kadınlar)
- Voleybol (Kadınlar)
- Badminton (Kadınlar)

**Kurallar:**
• TeamName UNIQUE olacak
• Aynı isimle iki takım oluşturulamaz
• Boş isimle kayıt yapılamaz
• **🔴 ÖNEMLİ:** Bir öğrenci aynı anda iki farklı takımda olamaz (tüm kategoriler için geçerli)
• Başarılı işlem sonrası toast mesaj gösterilir

### 12.8 Takıma Öğrenci Ekleme

**Arama Çubuğu:**
• Takım paneli içerisinde bulunur

**🆕 Arama Yöntemleri:**
• Numara ile arama
• Ad/Soyad ile arama

**Öğrenci Kartı:**
• Ana panel tasarımı ile aynı
• Favori butonu yok
• "Takıma Ekle" butonu var

**Kurallar:**
• Aynı öğrenci aynı takıma iki kez eklenemez
• **🔴 Öğrenci başka bir takımda varsa uyarı verilir ve ekleme engellenir**
• DB seviyesinde UNIQUE (TeamId + StudentId) constraint
• TeamMembers ilişki tablosu üzerinden çalışır
• Students tablosundan referans alınır (veri kopyalama yok)
• Başarılı eklemede toast mesaj

### 12.9 Takım İçindeki Öğrenci Kartı İçeriği

Takım açıldığında öğrenci kartları aşağı doğru listelenir.

**Kart İçeriği:**
• Foto
• Ad Soyad
• Sınıf
• TC No
• Doğum Tarihi (küçük font)
• "Detay" butonu

### 12.10 Takımdan Öğrenci Çıkarma

• Her öğrenci kartında "Takımdan Çıkar" butonu
• İşlem sonrası liste anlık güncellenir
• Başarılı işlem sonrası toast mesaj

### 12.11 Takım Silme Davranışı

• **Silme işlemi onay popup gerektirir**
• Teams tablosundaki kayıt silinir
• İlişkili TeamMembers kayıtları silinir
• Students tablosu etkilenmez
• İşlem transactional

**🆕 Takımlar sonradan üzerinden silebilinir (takım kartındaki sil butonu)**

### 12.12 Veri Bağlantısı

• Takım paneli Students tablosundaki verileri referans alır
• Takıma ekleme/çıkarma TeamMembers tablosu üzerinden
• **Veri kopyalama yapılmaz**

### 12.13 Kalıcılık

• Takımlar kalıcıdır
• TeamMembers kayıtları kalıcıdır
• Uygulama kapansa dahi veri korunur

### 12.14 Performans Kuralları

• Öğrenci kartları ilgili takım açıldığında yüklenir (lazy loading)
• UI donmaması için async veri çağrısı
• Çok sayıda takım olması durumunda arayüz karışmayacak
• Veri işlemleri UI thread'i bloklamaz

________________________________________
## 1️⃣3️⃣ TOAST MESAJ SİSTEMİ

### Özellikler:
• Büyük popup kullanılmaz
• Pozisyon: Sağ alt veya üst
• **Yeşil:** Başarılı işlem
• **Kırmızı:** Hata
• **Süre:** 3 saniye görünür

________________________________________
## 1️⃣4️⃣ VERİ KAYNAKLARI

### A) KÜNYE PDF

**Özellikler:**
• Çok sayfalı
• Her sayfa 1 öğrenci
• Format sabit
• Metin selectable
• Foto embedded image object

**Çekilecek Alanlar:**
• StudentNumber
• Ad Soyad
• TC No
• Doğum Tarihi
• Fotoğraf

**Foto İşleme:**
• En büyük image alınır
• Overwrite yapılır

### B) SINIF LİSTESİ PDF

• Tek PDF
• Tüm sınıflar mevcut
• Başlık formatı sabit

**Çekilecek:**
• StudentNumber
• Class

________________________________________
## 1️⃣5️⃣ KOD MİMARİSİ VE DOSYA/KLASÖR YAPISI (MODÜLER SİSTEM)

### Genel Beklenti:
Proje kodları kesinlikle tek bir dosyaya (monolithic yapı) yığılmayacak. Tüm sistem **fiziksel olarak ayrılmış, birbirinden bağımsız, modüler .cs ve .xaml dosyalarından** oluşacak.

**Temel Prensip:** "Görevler Ayrılığı" (Single Responsibility)

### Klasör ve Dosya Hiyerarşisi:

```
📁 AlparslanOBS/
├── 📁 Views/ (Arayüz Katmanı - XAML Dosyaları)
│   ├── LoginWindow.xaml
│   ├── MainWindow.xaml
│   ├── Components/
│   │   ├── HeaderComponent.xaml
│   │   ├── StudentCardComponent.xaml
│   │   ├── FavoritesPanelComponent.xaml
│   │   ├── ClassSelectionComponent.xaml
│   │   ├── DataInputPanelComponent.xaml
│   │   ├── ManagementButtonComponent.xaml
│   │   └── TeamPanelComponent.xaml
│
├── 📁 ViewModels/ (Arayüz Kontrol Katmanı)
│   ├── LoginViewModel.cs
│   ├── MainViewModel.cs
│   ├── TeamViewModel.cs
│   └── ...
│
├── 📁 Services/ (İş Mantığı Katmanı)
│   ├── MergeService.cs (UPSERT, Transaction, Normalizasyon)
│   ├── PdfExtractionService.cs (PDF okuma, metin/fotoğraf ayrıştırma)
│   ├── StudentService.cs
│   ├── TeamService.cs
│   ├── FavoriteService.cs
│   └── ...
│
├── 📁 DataAccess/ (Veritabanı Katmanı)
│   ├── DatabaseConnection.cs
│   ├── StudentRepository.cs
│   ├── TeamRepository.cs
│   ├── GuardianRepository.cs
│   └── ...
│
├── 📁 Models/ (Varlıklar)
│   ├── Student.cs
│   ├── Team.cs
│   ├── Guardian.cs
│   ├── TeamMember.cs
│   └── ...
│
└── 📁 Helpers/
    ├── PhotoHelper.cs
    ├── SlugifyHelper.cs
    └── ...
```

### Kodlama Standartları:

**Zorunlu Açıklama Satırları:**
Her sınıf, metod ve karmaşık algoritmanın üzerinde detaylı ve anlaşılır açıklama satırları (comments) bulunması zorunludur.

**İzolasyon:**
Veritabanına öğrenci ekleme kodu ile arayüzdeki bir animasyon kodu kesinlikle aynı dosyada bulunmayacak.

**Geliştirme Yaklaşımı:**
Geliştirici, modülleri kodlamaya başlamadan önce veya kritik entegrasyon adımlarında mutlaka onay alarak (mutabık kalarak) ilerleyecek.

**Genişleyebilirlik:**
Sistem, ileride yeni bir modül eklendiğinde mevcut kodları bozmadan sadece yeni klasör/dosya ekleyerek genişleyebilecek esneklikte olmalı.

________________________________________

## 📋 ÖZET KONTROL LİSTESİ

### ✅ Modüler Mimari:
- [ ] Header Modülü
- [ ] Ana Panel Modülü
- [ ] Student Card Component
- [ ] Favoriler Paneli Modülü (Sol alt, fade animasyonlu)
- [ ] Sınıf Seçim Modülü
- [ ] Bilgi Giriş Paneli Modülü
- [ ] Yönetim Paneli Butonu Modülü (Sağ alt, blurlu, hover efekti)
- [ ] Takım Yönetim Paneli Modülü
- [ ] Login Ekranı Modülü (Numpad + klavye desteği)

### ✅ Arayüz Standartları:
- [ ] 400x750 piksel standart boyut
- [ ] Katlanabilir yan menü (max 1000px)
- [ ] Topmost (Her zaman üstte)
- [ ] Taşınabilir (ancak resize edilemez)

### ✅ Takım Sistemi:
- [ ] 12 kategori (Erkek/Kadın: Futbol, Futsal, Basketbol, Hentbol, Voleybol, Badminton)
- [ ] Bir öğrenci birden fazla takımda olamaz kontrolü
- [ ] Takımlar sonradan silinebilir
- [ ] Accordion yapı (lazy loading)

### ✅ Performans:
- [ ] RAM optimizasyonu (IDisposable, DecodePixelWidth)
- [ ] Disk temizliği (temp files, silinen öğrencilerin fotoları)
- [ ] Rolling log
- [ ] Async veri çağrıları

### ✅ Veri Bütünlüğü:
- [ ] UPSERT (ON CONFLICT DO UPDATE)
- [ ] Foreign key constraints
- [ ] Transaction yönetimi
- [ ] Otomatik DELETE yasak

________________________________________

**SON GÜNCELLEME TARİHİ:** 2024
**DOSYA DURUMU:** ✅ Arayüz.md ile senkronize edildi
**VERSİYON:** 2.0 (Modüler Mimari + UI Detayları)
