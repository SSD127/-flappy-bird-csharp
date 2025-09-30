# Flappy Bird - Tek Dosya C# Oyunu

Bu proje tek dosyada C# Windows Forms kullanarak yapılmış basit bir Flappy Bird oyunudur.

## Özellikler

- 🐦 Kuş karakteri ve fizik sistemi
- 🚧 Rastgele yükseklikte borular
- 💥 Çarpışma algılama
- 📊 Skor sistemi
- 🎮 Basit kontroller (SPACE tuşu)
- 📁 Tek dosyada tüm kod

## Nasıl Çalıştırılır

### Gereksinimler
- Visual Studio Community (ücretsiz) veya .NET Framework Developer Pack

### Yöntem 1: Batch Dosyası ile (Kolay)
1. Visual Studio Community'yi indirin: https://visualstudio.microsoft.com/vs/community/
2. Kurulum sırasında ".NET desktop development" seçeneğini işaretleyin
3. `build.bat` dosyasına çift tıklayın
4. Derleme başarılı olduktan sonra `run.bat` dosyasına çift tıklayın

### Yöntem 2: Manuel Derleme
1. Terminal'de proje klasörüne gidin
2. Şu komutu çalıştırın:

```bash
csc /target:winexe /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll FlappyBird.cs
```

3. Oluşan `FlappyBird.exe` dosyasını çalıştırın

### Yöntem 3: Visual Studio ile
1. Visual Studio'da yeni bir Windows Forms projesi oluşturun
2. `FlappyBird.cs` dosyasının içeriğini kopyalayın
3. F5 ile çalıştırın

## Oyun Kontrolleri

- **SPACE**: Kuşu zıplat
- **SPACE** (oyun bittiğinde): Oyunu yeniden başlat

## Oyun Kuralları

- Kuş sürekli aşağı düşer (yerçekimi)
- SPACE tuşuna basarak kuşu yukarı zıplatın
- Borulara çarpmamaya çalışın
- Her geçilen boru için 1 puan kazanın
- Yere çarparsanız veya ekranın üstüne çıkarsanız oyun biter

## Teknik Detaylar

- **Framework**: .NET 6.0
- **UI**: Windows Forms
- **Dil**: C#
- **Oyun Döngüsü**: 50 FPS (20ms interval)

## Dosya Yapısı

- `FlappyBird.cs` - Tek dosyada tüm oyun kodu
  - `Program` - Ana giriş noktası
  - `FlappyBirdGame` - Ana oyun formu ve oyun döngüsü
  - `Bird` - Kuş karakteri sınıfı
  - `Pipe` - Boru sınıfı
  - `PipeManager` - Boru yönetimi sınıfı

## Geliştirme Notları

Bu oyun eğitim amaçlı yapılmıştır ve temel oyun programlama konseptlerini göstermektedir:
- Oyun döngüsü
- Fizik simülasyonu
- Çarpışma algılama
- Oyun durumu yönetimi
