# বেবি কীবোর্ড স্ম্যাশ (BabyKeyBoardSmashByMahfoz)

ছোটদের জন্য ফুলস্ক্রিন নিরাপদ **বাংলা বর্ণমালা ও সংখ্যার** মজার কীবোর্ড স্ম্যাশ গেম — Scott Hanselman-এর [BabySmash](https://github.com/shanselman/babysmash) থেকে অনুপ্রাণিত হয়ে সম্পূর্ণ নতুনভাবে তৈরি, যেখানে প্রতিটি অক্ষর, সংখ্যা, ইমোজি ও উচ্চারণ সম্পূর্ণ বাংলায় উপস্থাপিত।

ছোট বাচ্চারা যখন কীবোর্ডে বাটন চাপবে: প্রতিটি ক্লিকে স্ক্রিনে বড় আকারের অ্যানিমেটেড বাংলা বর্ণ বা সংখ্যা, মানানসই রঙিন ওপেন-সোর্স ইমোজি, মজার সাউন্ড এফেক্ট এবং মিষ্টি বাংলা ভয়েস উচ্চারণ বেজে উঠবে। অ্যাপটি ব্যাকগ্রাউন্ডে উইন্ডোজ কি (Win Key), Alt+Tab এবং Sticky/Filter Keys ব্লক করে রাখে যাতে কম্পিউটারের ফাইল বা উইন্ডো ক্ষতিগ্রস্ত না হয়। বড়দের বের হওয়ার জন্য রয়েছে **Alt+F4** অথবা **Escape (২ সেকেন্ড চেপে রাখা)**।

---

## 🚀 সাধারণ ব্যবহারকারীদের জন্য ইনস্টলেশন (One-Click Installer)

প্রোগ্রামিং বা ডটনেট জ্ঞান ছাড়াই যে কেউ সহজে এটি ইনস্টল করতে পারবেন:

1. `dist/BabyKeyBoardSmash_Setup.exe` ফাইলে ডাবল ক্লিক করুন।
2. ইনস্টলার উইন্ডোতে **"ইনস্টল করুন (Install)"** বাটনে ক্লিক করুন (কোনো অ্যাডমিনিস্ট্রেটর পারমিশন বা UAC লাগবে না)।
3. ইনস্টলেশন শেষে স্বয়ংক্রিয়ভাবে আপনার **Desktop** ও **Start Menu**-তে শর্টকাট তৈরি হবে এবং অ্যাপটি চালু হবে।
4. পরবর্তীতে আনইনস্টল করতে চাইলে উইন্ডোজের *Settings > Installed Apps* অথবা ফোল্ডারের `Uninstall.exe` থেকে এক ক্লিকেই মুছে ফেলা যাবে।

> **পোর্টেবল ভার্সন (Portable Version)**: আপনি ইনস্টল না করতে চাইলে সরাসরি `dist/app/BabyKeyBoardSmash.exe` চালু করেও ব্যবহার করতে পারেন।

---

## ✨ বৈশিষ্ট্যসমূহ (Features)

- **ফুলস্ক্রিন ও টডলার-প্রুফ (Toddler-Proof Lock)**: ফুলস্ক্রিন মোডে চলে, উইন্ডোজের বিশেষ কি-গুলো ব্লক থাকে।
- **বাংলা বর্ণ ও সংখ্যা (Bangla Letters & Numbers)**: `০–৯` চাপলে বাংলায় `০–৯` এবং `A–Z` বা অন্যান্য কি চাপলে স্বরবর্ণ, ব্যঞ্জনবর্ণ ও শব্দ প্রকাশ পায়।
- **বাক্সবিহীন সুন্দর কার্ড ভিউ (Centered Dynamic Card Layout)**: বড় অক্ষরের সাথে বড় রঙিন ইমোজি ও গোল্ডেন পিল ব্যাজে বাংলা শব্দ (যেমন: **অ — অজগর**, **ব — বই**, **ক — কলম**)।
- **শব্দ ও উচ্চারণ (Spoken Voice Pronunciation)**: অক্ষরের সাথে সংশ্লিষ্ট শব্দের পূর্ণ উচ্চারণ (যেমন: "অ — অজগর") স্পষ্ট ও মিষ্টি কণ্ঠে শোনা যায়।
- **অন্যান্য কী চাপলে শেপ ও মজার সাউন্ড (Shapes for Special Keys)**: স্পেস, এন্টার বা অন্যান্য কি চাপলে রঙিন স্টার, হার্ট, ট্রায়াঙ্গেল ইত্যাদি শেপ এবং মজার কার্টুন সাউন্ড এফেক্ট আসে।
- **স্মার্ট এক্সিট (Smart Safe Exit)**: ছোটদের ভুলবশত বের হয়ে যাওয়া রুখতে **Escape বাটন ২ সেকেন্ড চেপে ধরলে** অথবা বড়রা **Alt+F4** চাপলে অ্যাপ বন্ধ হয়।
- **১০০% অফলাইন ও নিরাপদ**: কোনো ইন্টারনেট বা ট্র্যাকিং নেই।

---

## 🛠️ ডেভেলপার ও সোর্স কোড বিল্ড (For Developers)

- **প্রয়োজনীয়তা**: Windows 10 বা 11, [.NET 8 SDK](https://dotnet.microsoft.com/download)

```powershell
git clone https://github.com/mahfozmt/BabyKeyBoardSmashByMahfoz.git
cd BabyKeyBoardSmashByMahfoz

# প্রজেক্ট রান করতে:
dotnet run --project src/BabySmashBN

# সম্পূর্ণ সলিউশন বিল্ড করতে:
dotnet build BabyKeyBoardSmashByMahfoz.slnx

# টেস্ট রান করতে:
dotnet publish tests/BabySmashBN.Tests -c Release -r win-x64 --self-contained -o tests/bin/Release
& "tests/bin/Release/BabySmashBN.Tests.exe"
```

---

## 📁 প্রজেক্ট স্ট্রাকচার (Project Structure)

```
BabyKeyBoardSmashByMahfoz/
├─ BabyKeyBoardSmashByMahfoz.slnx    # Visual Studio / .NET সলিউশন ফাইল
├─ dist/
│  ├─ BabyKeyBoardSmash_Setup.exe    # সাধারণ ব্যবহারকারীদের জন্য ওয়ান-ক্লিক ইনস্টলার
│  └─ app/                           # পোর্টেবল বিল্ড (BabyKeyBoardSmash.exe + Assets/)
├─ src/
│  ├─ BabySmashBN.Core/              # কি-ম্যাপিং, মডেল ও Win32 হুকস
│  ├─ BabySmashBN/                   # মূল WPF অ্যাপ্লিকেশন (UI, সাউন্ড ও অ্যানিমেশন)
│  └─ BabyKeyBoardSmash.Installer/   # স্বয়ংক্রিয় ইনস্টলার ও আনইনস্টলার প্রজেক্ট
├─ tests/
│  └─ BabySmashBN.Tests/             # স্বয়ংক্রিয় ইউনিট টেস্ট
├─ installer/
│  └─ setup.iss                      # Inno Setup স্ক্রিপ্ট (CI/CD-র জন্য)
├─ tools/
│  └─ AssetGenerator/                # অ্যাসেট ভ্যালিডেশন টুল
├─ ASSETS_LICENSES.md                # ফন্ট, ইমোজি ও সাউন্ডের ওপেন-সোর্স লাইসেন্স
└─ LICENSE                           # MIT License
```

---

## 🛑 অ্যাপ বন্ধ করার নিয়ম (How to Exit)

- **Alt+F4** চাপুন, অথবা
- **Escape** বাটনটি একটানা **২ সেকেন্ড চেপে ধরে রাখুন**।

---

## 📄 লাইসেন্স (License)

কোড [MIT License](LICENSE)-এর আওতায় উন্মুক্ত। ব্যবহৃত ফন্ট, ইমোজি ও অডিও অ্যাসেটসমূহ ওপেন-সোর্স লাইসেন্সধারী (বিস্তারিত দেখুন [ASSETS_LICENSES.md](ASSETS_LICENSES.md))।
