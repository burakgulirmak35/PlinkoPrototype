🎮 Oynanış (Core Gameplay)

Oyuncu ekrana basılı tutarak Plinko toplarını bırakır.

Toplar peg’lere çarparak aşağı düşer.

Alt kısımdaki Bucket’lara giren her top bir ödül üretir.

Oyuncu başlangıçta 200 top ile başlar.

Belirli sayıda top skora ulaştığında:

Level tamamlanır

Bir sonraki level yüklenir

📈 İlerleme Sistemi (Progression)

Her level JSON dosyası ile tanımlanır.

Level verileri:

Bucket sayısı

Bucket skorları

Level geçmek için gereken top sayısı

Level atlandıkça:

Bucket dizilimi değişir

Daha yüksek ödüller sunulur

Bu yapı data-driven olduğu için:

Kod değiştirmeden yeni level eklenebilir

Designer-friendly bir yapı sunar

🧠 Mimari Yaklaşım
Event-Driven Sistem

Oyun akışı, merkezi bir GameEvents yapısı üzerinden ilerler:

Input

Ball spawn

Skor

Level geçişi

UI güncellemeleri

Bu sayede:

Sistemler birbirine tightly-coupled değildir

UI → Gameplay polling yapılmaz

Performans kaybı önlenir

Ana Sistemler
Sistem	Sorumluluk
GameManager	Oyun state’leri, UI, level akışı
BallManager	Object pooling, spawn, fizik
LevelManager	JSON’dan runtime level üretimi
PlayerDataManager	Kalıcılık & reset
RewardValidator	Ödül toplama & doğrulama
MockServerService	Backend simülasyonu
🔐 Ödül Doğrulama Stratejisi (Case’in En Kritik Kısmı)
Problem (Case’te Tanımlanan)

Client tarafında hesaplanan ödül güvenilmez

Her top için server isteği atmak performanssız

Çözüm (Bu Projede)

Batch-based + Server-authoritative yaklaşım:

Her top düştüğünde client tarafında RewardPackage oluşturulur

UI optimistic olarak güncellenir

Ödüller:

Belirli sayıya ulaştığında

Belirli süre geçtiğinde

Level sonunda
batch halinde server’a gönderilir

Server:

Aynı topun iki kez işlenmesini engeller (BallId)

Wallet’ı authoritative şekilde günceller

Client, server’dan gelen wallet ile senkronize olur

Bu yapı:

Güvenliği sağlar

Network spam’i engeller

Kullanıcı deneyimini bozmaz

🧪 Mock Backend (Server Simülasyonu)

Gerçek backend yerine MockServerService kullanılmıştır:

Task.Delay ile network latency simülasyonu

Authoritative wallet

Duplicate reward engelleme

Player state persistence

Case gereği, servis boş stub değildir, tüm mantık çalışır durumdadır.

⏱ Zaman Bazlı Reset & Kalıcılık

Oyun her 15 dakikada bir resetlenir

Reset sırasında:

Level ve top sayısı sıfırlanır

Wallet ve reward history korunur

Reset süresi:

Oyun kapatılıp açılsa bile tutarlı çalışır

UI’da geri sayım olarak gösterilir

⚡ Performans Önlemleri

Object Pooling:

Plinko Ball

CoinText

History Entry

Event-driven UI güncellemeleri

Minimal allocation

GC pressure minimize edilmiştir

5–10 top/sn senaryosunda stabil çalışacak şekilde tasarlanmıştır

🛠 Editor Araçları
Level Creator Window

Unity Editor içinde geliştirilen custom tool:

Level oluşturma

Var olan level’ı JSON’dan yükleme

Bucket skor & renk düzenleme

Tek tuşla JSON export

🖥 Debug & Görselleştirme

RewardValidator Debug HUD:

Pending reward sayısı

Local vs Server wallet

Son batch zamanı

Latency aralığı

Bu HUD, sistemin doğru çalıştığını görsel olarak kanıtlamak için eklenmiştir.

▶ Çalıştırma

Unity 2022+ ile projeyi aç

StreamingAssets/Levels klasörünü kontrol et

Ana sahneyi aç

Play

🏁 Sonuç

Bu proje:

Case’te istenen tüm teknik gereksinimleri karşılar

Gerçek mobil oyun mimarilerini simüle eder

Performans, güvenlik ve ölçeklenebilirliği önceliklendirir
