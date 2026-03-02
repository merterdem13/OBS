PDF Regex Kuralları

I.	Öğrenci Künye Defteri
•	Künye defterleri .pdf formatında yüklenir.
•	Başlığında “ÖĞRENCİ KÜNYE DEFTERİ” olarak unique biçimde (diğer pdf’lerden ayrı” başlık bulunur.
•	Çekilmesi gereken metin verileri: “Okul No: ”, “Adı: “, “Soyadı: “, “Doğum Yeri , Tarihi: “, “T.C. Kimlik No: “.
•	Okul No verisi örnek olarak 12,183,456 gibi gibi veriler olabilir.
•	Adı verisi: Öğrencinin adı, Türkçe karakterler içerebilir.
•	Soyadı verisi: Öğrencinin soyadı, Türkçe karakterler içerebilir.
•	Doğum Yeri , Tarihi verisi: “doğumyeri” , “dd/mm/yy” formatında doğum tarihi içerir. Önemli not: sadece doğum tarihi verisini çekeceğiz.
•	T.C. Kimlik No verisi: 11 haneli kimlik numarası verisidir. Bunu çekeceğiz.
•	Çekilmesi gereken resim verisi: PDF’te sadece 2 resim bulunur. Biri vesikalık gibi öğrencinin fotoğrafıdır, diğeri ise e-okula ait bulanık bir logo. Boyutu yüksek olan resmi çecekeğiz ve bu da öğrencinin fotoğrafı olacak.
•	Defter’den başka herhangi bir veri çekilmez. Defter’de ki veriler sabittir (her öğrenci için farklı tabii ki ama şematik olarak pdf’ler değişmez.)
•	ÖĞRENCİ KÜNYE DEFTERİ olarak sistemce ayırt edilen pdfler taranır, taranan ve sisteme import edilmiş olan pdfler aynı zamanda “{NO}-{AD-SOYAD}” biçiminde fiziksel olarak kaydedilmelidir. Resimler’de aynı biçimde kaydedilmelidir.
•	Künye defterleri, öğrenci kartlarından açılabilmeli.
II.	Sınıf Listesi
•	Sınıf Listeleri .pdf formatında yüklenir.
•	Başlığında “{Sınıf numarası}. Sınıf / {Şubesini belirten harf} Şubesi Sınıf Listesi” diye unique biçimde başlık bulunur.
•	Çekilmesi gereken ve matchlenmesi gereken veriler: Başlıktan örneğin “7/F” diye veriyi çekip listede bulunan öğrenciler örneğin: 12 numaralı X öğrenciye bu sınıftan olduğunu matchlemeli. Ayrıca; Numarasını matchlediğimiz öğrencilerin, aynı satırda bulunan en son sütününda "Cinsiyeti" bulunur. "Kız" veya "Erkek" bu verileri de çekip matchleyeceğiz çünkü Team Oluşturma kısmında bu verilere ihtiyacımız olacak.
•	Liste fiziksel olarak “{SINIF}-{ŞUBE}-Listesi” biçiminde kaydedilmeli.
•	Sınıf bazlı filtrelemelerde/aramalarda gösterilebilir olması gerekiyor. Görüntüleme yani.

III.	Resim Listesi
•	Resim Listeleri .pdf formatında yüklenir.
•	Başlığında “{sınıf numarası}. Sınıf / {şubeyi belirten harf} Şubesi” bulunur. Sınıf listesinden farkı resim listesi olduğu belirtilmiyor.
•	Herhangi bir veri çekilmeyecek. Sadece Sınıf bazlı arama/filtreleme durumunda resim listesinin görüntülenebilir olması gerekiyor.
•	Liste fiziksel olarak “{SINIF}-{ŞUBE}-Resim_Listesi” biçiminde kaydedilmeli.

Küçük notlar:
Tek bir yükleme butonu olacağı için, başlıkların önemi büyük. Başlıklar tüm listelerde sabit.
Künye defterinde göründüğü üzere nokta atışı bir teknik uyguluyoruz. Örneğin: “Öğrenci No: “ dan sonra öğrencinin numarası bulunur. Bu hepsinde sabittir.







