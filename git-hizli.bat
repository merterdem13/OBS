@echo off
:: UTF-8 kod sayfasina gecis yaparak Turkce karakter destegi saglar
chcp 65001 > nul
setlocal enabledelayedexpansion
title Git Otomasyon Paneli
color 0B

:: Mevcut Branch Adini Al
for /f "tokens=*" %%i in ('git rev-parse --abbrev-ref HEAD') do set branch=%%i

:menu
cls
echo ======================================================
echo           GIT HIZLI YÖNETİM PANELİ (v2.1)
echo ======================================================
echo  Mevcut Branch: [%branch%]
echo ------------------------------------------------------
echo  [1] Mevcut Branch'e Pushla (%branch%)
echo  [2] Yeni Branch Oluştur ve Ona Pushla
echo  [3] Branch Değiştir (Checkout)
echo  [4] Değişiklikleri Gör (Status)
echo  [0] Çıkış
echo ------------------------------------------------------
set /p secim="Seçiminizi yapın: "

if "%secim%"=="1" goto push_current
if "%secim%"=="2" goto new_branch
if "%secim%"=="3" goto switch_branch
if "%secim%"=="4" goto show_status
if "%secim%"=="0" exit
goto menu

:push_current
echo.
echo Değişiklikler hazırlanıyor...
git add .
set /p msg="Commit mesajı girin (Boşsa 'Oto-Guncelleme'): "
if "%msg%"=="" set msg=Oto-Guncelleme - %date% %time%

git commit -m "%msg%"
echo.
echo Sunucuya gönderiliyor: %branch%...
git push origin master --force %branch%
echo.
echo İşlem başarıyla tamamlandı!
pause
goto menu

:new_branch
echo.
set /p nb="Yeni branch adını girin (Örn: feature-login): "
git checkout -b %nb%
set branch=%nb%
echo Branch oluşturuldu ve geçiş yapıldı.
goto push_current

:switch_branch
echo.
echo --- MEVCUT BRANCHLER ---
git branch
echo -----------------------
set /p sb="Geçmek istediğiniz branch adı: "
git checkout %sb%
:: Branch adini guncelle
for /f "tokens=*" %%i in ('git rev-parse --abbrev-ref HEAD') do set branch=%%i
goto menu

:show_status
echo.
echo --- MEVCUT DURUM ---
git status -s
echo --------------------
pause
goto menu