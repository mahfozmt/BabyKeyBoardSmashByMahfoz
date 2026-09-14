# Contributing to BabyKeyBoardSmashByMahfoz

বাংলায় তৈরি এই উন্মুক্ত ওপেন-সোর্স প্রোজেক্টটিতে আপনার অবদান সাদরে গ্রহণযোগ্য! 👶✨  
Thank you for your interest in contributing to **BabyKeyBoardSmashByMahfoz**!

---

## 🌟 অবদান রাখার নিয়ম (How to Contribute)

যেহেতু মূল রিপোজিটরির মূল ব্রাঞ্চটি সুরক্ষিত (Protected Branch), তাই সরাসরি ব্রাঞ্চে পুশ করার পরিবর্তে স্ট্যান্ডার্ড **Fork & Pull Request** পদ্ধতি অনুসরণ করতে হয়:

### ১. ফর্ক ও ক্লোন করুন (Fork & Clone)
1. GitHub পেজের ওপরের ডান কোণায় থাকা **Fork** বাটনে ক্লিক করে আপনার অ্যাকাউন্টে একটি কপি তৈরি করুন।
2. আপনার ফর্কটি কম্পিউটারে ক্লোন করুন:
   ```bash
   git clone https://github.com/<your-username>/BabyKeyBoardSmashByMahfoz.git
   cd BabyKeyBoardSmashByMahfoz
   ```

### ২. নতুন ব্রাঞ্চ তৈরি করুন (Create a Feature Branch)
কখনোই সরাসরি `master` ব্রাঞ্চে কাজ করবেন না; অর্থপূর্ণ নামে একটি নতুন ব্রাঞ্চ তৈরি করুন:
```bash
git checkout -b feature/add-new-sound
# অথবা
git checkout -b fix/window-scaling-bug
```

### ৩. কোডিং ও ডেভেলপমেন্ট (Development)
- **প্রয়োজনীয়তা**: Windows 10/11 এবং [.NET 8 SDK](https://dotnet.microsoft.com/download)।
- ডেভেলপমেন্ট মোডে রান করে পরীক্ষা করুন:
  ```powershell
  dotnet run --project src/BabySmashBN
  ```

### ৪. স্বয়ংক্রিয় টেস্ট চালান (Run Automated Tests)
যেকোনো পরিবর্তনের পর নিশ্চিত করুন যে সমস্ত ইউনিট টেস্ট সফলভাবে পাস করেছে:
```powershell
dotnet publish tests/BabySmashBN.Tests -c Release -r win-x64 --self-contained -o tests/bin/Release
& "tests/bin/Release/BabySmashBN.Tests.exe"
```

### ৫. কমিট ও পুশ (Commit & Push)
```bash
git add .
git commit -m "feat: add cute cartoon chime on space key"
git push origin feature/add-new-sound
```

### ৬. পুল রিকোয়েস্ট তৈরি করুন (Open a Pull Request)
1. GitHub-এ আপনার ফর্ক রিপোজিটরিতে যান।
2. উপরে **"Compare & pull request"** বাটনে ক্লিক করুন।
3. আপনার পরিবর্তনসমূহের একটি সংক্ষিপ্ত বিবরণ দিন এবং **Create pull request**-এ ক্লিক করুন।
4. রিপোজিটরির মেইনটেইনার (Mahfoz) আপনার কোড রিভিউ করবেন এবং মার্জ করবেন।

---

## 🎨 অ্যাসেট নীতি (Asset Licensing Guidelines)
- প্রোজেক্টটিতে ব্যবহৃত প্রতিটি ফন্ট, ইমোজি এবং অডিও ক্লিপ অবশ্যই **উন্মুক্ত ও পারমিশনপ্রাপ্ত ওপেন-সোর্স লাইসেন্সধারী** (MIT, Apache 2.0, SIL Open Font License, CC0, ইত্যাদি) হতে হবে।
- কোনো কপিরাইটেড বা পেইড মিডিয়া ফাইল যুক্ত করা যাবে না।
- নতুন কোনো অ্যাসেট যুক্ত করলে তা অবশ্যই [`ASSETS_LICENSES.md`](ASSETS_LICENSES.md) ফাইলে লাইসেন্স সূত্রসহ উল্লেখ করতে হবে।

---

## 💡 আইডিয়া বা বাগ রিপোর্ট (Issues & Discussions)
কোডিং ছাড়াও নতুন কোনো বাংলা ছড়া, ইমোজি আইডিয়া, বাগ রিপোর্ট বা পরামর্শের জন্য নির্দ্বিধায় [GitHub Issues](https://github.com/mahfozmt/BabyKeyBoardSmashByMahfoz/issues)-এ ইস্যু ওপেন করুন!
