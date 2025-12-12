## 🎮 Oynanış (Core Gameplay)

- Oyuncu ekrana **basılı tutarak** Plinko toplarını bırakır.
- Toplar peg’lere çarparak aşağı düşer.
- Alt kısımdaki **Bucket**’lara giren her top bir ödül üretir.
- Oyuncu başlangıçta **200 top** ile başlar.
- Belirli sayıda top skora ulaştığında:
  - Level tamamlanır
  - Bir sonraki level yüklenir

---

## 📈 İlerleme Sistemi (Progression)

- Her level **JSON dosyası** ile tanımlanır.
- Level verileri:
  - Bucket sayısı
  - Bucket skorları
  - Level geçmek için gereken top sayısı

- Level atlandıkça:
  - Bucket dizilimi değişir
  - Daha yüksek ödüller sunulur

Bu yapı **data-driven** olduğu için:
- Kod değiştirmeden yeni level eklenebilir
- Designer-friendly bir yapı sunar

---

## 🧠 Mimari Yaklaşım

### Event-Driven Sistem

Oyun akışı, merkezi bir **GameEvents** yapısı üzerinden ilerler:

- Input
- Ball spawn
- Skor
- Level geçişi
- UI güncellemeleri

Bu sayede:
- Sistemler birbirine **tightly-coupled değildir**
- UI → Gameplay polling yapılmaz
- Performans kaybı önlenir

---

### Ana Sistemler

| Sistem | Sorumluluk |
|------|------------|
| GameManager | Oyun state’leri, UI, level akışı |
| BallManager | Object pooling, spawn, fizik |
| LevelManager | JSON’dan runtime level üretimi |
| PlayerDataManager | Kalıcılık & reset |
| RewardValidator | Ödül toplama & doğrulama |
| MockServerService | Backend simülasyonu |

---

## 🔐 Ödül Doğrulama Stratejisi  
*(Case’in En Kritik Kısmı)*

### Problem (Case’te Tanımlanan)

- Client tarafında hesaplanan ödül **güvenilmez**
- Her top için server isteği atmak **performanssız**

### Çözüm (Bu Projede)

**Batch-based + Server-authoritative yaklaşım:**

1. Her top düştüğünde client tarafında **RewardPackage** oluşturulur
2. UI **optimistic** olarak güncellenir
3. Ödüller:
   - Belirli sayıya ulaştığında
   - Belirli süre geçtiğinde
   - Level sonunda  
   batch halinde server’a gönderilir
4. Server:
   - Aynı topun iki kez işlenmesini engeller (**BallId**)
   - Wallet’ı **authoritative** şekilde günceller
5. Client, server’dan gelen wallet ile senkronize olur

Bu yapı:
- Güvenliği sağlar
- Network spam’i engeller
- Kullanıcı deneyimini bozmaz

---

## 🧪 Mock Backend (Server Simülasyonu)

Gerçek backend yerine **MockServerService** kullanılmıştır:

- `Task.Delay` ile **network latency simülasyonu**
- Authoritative wallet
- Duplicate reward engelleme
- Player state persistence

> Case gereği servis **boş stub değildir**, tüm mantık çalışır durumdadır.

---

## ⏱ Zaman Bazlı Reset & Kalıcılık

- Oyun **her 15 dakikada bir** resetlenir
- Reset sırasında:
  - Level ve top sayısı sıfırlanır
  - Wallet ve reward history **korunur**

- Reset süresi:
  - Oyun kapatılıp açılsa bile tutarlı çalışır
  - UI’da geri sayım olarak gösterilir

---

## ⚡ Performans Önlemleri

- **Object Pooling**
  - Plinko Ball
  - CoinText
  - History Entry
- Event-driven UI güncellemeleri
- Minimal allocation
- GC pressure minimize edilmiştir
- 5–10 top/sn senaryosunda stabil çalışacak şekilde tasarlanmıştır

---

## 🛠 Editor Araçları

### Level Creator (Unity Editor Tool)

<br/>

<img width="489" height="475" alt="Level Creator Tool"
src="https://github.com/user-attachments/assets/65f10ea7-849b-49d2-b5e9-116b66cb526c" />

Bu projede, level içeriklerinin **koddan bağımsız** olarak üretilebilmesi için özel bir  
**Unity Editor aracı (Level Creator)** geliştirilmiştir.

Bu araç sayesinde:

- Level ID üzerinden mevcut bir level **JSON dosyasından yüklenebilir**
- Bucket sayısı dinamik olarak ayarlanabilir
- Her bucket için:
  - **Skor değeri**
  - **Renk (hex formatında)**
  görsel arayüz üzerinden düzenlenebilir
- Level geçmek için gereken top sayısı belirlenebilir
- Tek tuşla level verisi **StreamingAssets/Levels** klasörüne JSON olarak kaydedilir

Bu yapı **data-driven** olarak tasarlanmıştır.  
Mevcut implementasyonda level verileri lokal JSON dosyalarından okunmaktadır; ancak aynı yapı **backend üzerinden** de servis edilebilecek şekilde kurgulanmıştır.

Bu sayede:
- Oyunu güncellemeden **level dengeleri değiştirilebilir**
- Yeni level’lar **remote config / backend** üzerinden eklenebilir
- **A/B test**, **live-ops** ve hızlı dengeleme senaryoları desteklenir

Bu yaklaşım, gerçek projelerde kullanılan **live-ops uyumlu içerik yönetimi** ve  
**ölçeklenebilir level pipeline** mantığını yansıtmaktadır.

---

## 🖥 Debug & Görselleştirme

**RewardValidator Debug HUD**:

- Pending reward sayısı
- Local vs Server wallet
- Son batch zamanı
- Latency aralığı

Bu HUD, sistemin doğru çalıştığını **görsel olarak kanıtlamak** için eklenmiştir.

---

## ▶ Çalıştırma

1. Unity **2022+** ile projeyi aç
2. `StreamingAssets/Levels` klasörünü kontrol et
3. Ana sahneyi aç
4. Play

---

## 🏁 Sonuç

Bu proje:

- Case’te istenen tüm teknik gereksinimleri karşılar
- Gerçek mobil oyun mimarilerini simüle eder
- Performans, güvenlik ve ölçeklenebilirliği önceliklendirir

Amaç, yalnızca çalışan bir Plinko üretmek değil;  
**üretim ortamına hazır bir sistem yaklaşımı** sunmaktır.
