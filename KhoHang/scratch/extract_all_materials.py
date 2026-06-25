import pandas as pd
import openpyxl
import json

def clean_name(name):
    if name is None: return None
    name = str(name).strip()
    if not name or name.lower() in ['nan', 'none', 'tên hàng', 'đvt', 'sl', 'đơn giá', 'thành tiền', 'tổng cộng', 'tổng công', 'ghi chú', 'cộng', 'ngày', 'trả đủ:', 'trả đủ :', 'trả dư', 'còn lại', 'phí giao', 'phí vận chuyển']: 
        return None
    if name.upper().startswith('A') and len(name) < 3: return None
    
    # Group 1 Normalizations (as requested by user)
    if name == "DT 86N": name = "DT86N"
    if name == "Phí VC :": name = "Phí VC"
    if name == "Sắt 10V": name = "Sắt phi 10V" # Sync with C# code
    
    return name

def extract_all():
    results = []
    
    # 1. Sổ làm việc.xlsx (Using Pandas)
    file_so_lam_viec = r'd:\Download\Hoc\KhoHang\Sổ làm việc.xlsx'
    xl_so = pd.ExcelFile(file_so_lam_viec)
    cat_map = {
        '20x40': 'Gạch men 20x40', '25x40': 'Gạch men 25x40', '30x45': 'Gạch men 30x45',
        '30x60': 'Gạch men 30x60', '40x80': 'Gạch men 40x80', '30x30': 'Gạch men 30x30',
        '40x40': 'Gạch men 40x40', '50x50': 'Gạch men 50x50', '60x60': 'Gạch men 60x60',
        '80x80': 'Gạch men 80x80', 'HT d? + insee': 'Xi măng & Bê tông',
        'HT 2 ln': 'Xi măng & Bê tông', 'HT 1 ln': 'Xi măng & Bê tông'
    }
    for sheet_name in xl_so.sheet_names:
        matched_cat = next((v for k, v in cat_map.items() if k in sheet_name.strip()), None)
        if not matched_cat: continue
        try:
            df = pd.read_excel(file_so_lam_viec, sheet_name=sheet_name, header=None)
            for index, row in df.iterrows():
                if len(row) >= 2:
                    name = clean_name(row[1])
                    if name:
                        results.append({'Name': name, 'Category': matched_cat, 'Unit': 'm' if 'Gạch men' in matched_cat else 'bao', 'Price': 0, 'Supplier': None})
        except: pass

    # 2. 1 A ĐĂT HÀNG.xlsx (Using Pandas)
    file_dat_hang = r'd:\Download\Hoc\KhoHang\1                     A  ĐĂT HÀNG.xlsx'
    xl_dat = pd.ExcelFile(file_dat_hang)
    for sheet_name in xl_dat.sheet_names:
        try:
            df = pd.read_excel(file_dat_hang, sheet_name=sheet_name, header=None)
            for index, row in df.iterrows():
                if len(row) >= 7:
                    name = clean_name(row[3])
                    unit = clean_name(row[4])
                    price = row[6]
                    if name:
                        found = False
                        for r in results:
                            if r['Name'].lower() == name.lower():
                                if pd.notna(price) and isinstance(price, (int, float)): r['Price'] = float(price)
                                if unit and len(str(unit)) < 10: r['Unit'] = str(unit)
                                r['Supplier'] = sheet_name
                                found = True
                                break
                        if not found:
                            category = 'Khác'
                            lname = name.lower()
                            if '60x60' in lname or '60' in lname: category = 'Gạch men 60x60'
                            elif '30x60' in lname: category = 'Gạch men 30x60'
                            elif '80' in lname: category = 'Gạch men 80x80'
                            elif 'xi măng' in lname: category = 'Xi măng & Bê tông'
                            elif '25x40' in lname: category = 'Gạch men 25x40'
                            elif '40x40' in lname: category = 'Gạch men 40x40'
                            elif 'cát' in lname: category = 'Cát'
                            elif 'đá' in lname: category = 'Đá, Sỏi'
                            elif 'vận chuyển' in lname or 'phí vc' in lname: category = 'Gạch xây dựng'
                            results.append({'Name': name, 'Category': category, 'Unit': unit if unit and len(str(unit)) < 10 else ('m' if 'Gạch' in category else 'bao'), 'Price': float(price) if pd.notna(price) and isinstance(price, (int, float)) else 0, 'Supplier': sheet_name})
        except: pass

    # 3. GẠCH ỐNG.xlsx (Using Openpyxl)
    file_gach_ong = r'd:\Download\Hoc\KhoHang\GẠCH ỐNG.xlsx'
    try:
        wb = openpyxl.load_workbook(file_gach_ong, data_only=True)
        ws = wb.active
        current_supplier = None
        for row in ws.iter_rows(min_row=1):
            val2 = row[2].value
            val3 = row[3].value
            val5 = row[5].value
            val6 = row[6].value
            name2 = clean_name(val2)
            name3 = clean_name(val3)
            
            if name2 and not name3:
                if "LÒ" in name2.upper() or "GHE" in name2.upper() or "ANH" in name2.upper():
                    current_supplier = name2
                else:
                    name = name2
                    price = val5 if isinstance(val5, (int, float)) else 0
                    if not price and isinstance(val6, (int, float)) and isinstance(row[4].value, (int, float)) and row[4].value > 0:
                        price = val6 / row[4].value
                    if name:
                        results.append({'Name': name, 'Category': 'Gạch xây dựng', 'Unit': 'viên', 'Price': float(price), 'Supplier': current_supplier})
            elif name3:
                name = name3
                price = val5 if isinstance(val5, (int, float)) else 0
                if not price and isinstance(val6, (int, float)) and isinstance(row[4].value, (int, float)) and row[4].value > 0:
                    price = val6 / row[4].value
                if name:
                    results.append({'Name': name, 'Category': 'Gạch xây dựng', 'Unit': 'viên', 'Price': float(price), 'Supplier': current_supplier})
    except Exception as e:
        print(f"Error reading GẠCH ỐNG: {e}")

    # Deduplicate
    unique_results = {}
    for r in results:
        key = r['Name'].lower()
        if key not in unique_results or (unique_results[key]['Price'] == 0 and r['Price'] > 0):
            unique_results[key] = r
    final_list = list(unique_results.values())
    
    # Final cleanup
    for r in final_list:
        if 'Gạch men' in r['Category']: r['Unit'] = 'm'
        if r['Unit'] is None: r['Unit'] = 'viên' if 'Gạch xây dựng' in r['Category'] else 'bao'
            
    final_list.sort(key=lambda x: (x['Category'], x['Name']))

    with open(r'd:\Download\Hoc\KhoHang\KhoHang\scratch\materials_cs.txt', 'w', encoding='utf-8') as f:
        for r in final_list:
            supplier = f'"{r["Supplier"]}"' if r["Supplier"] else "(string)null"
            f.write(f'            new {{ Name = "{r["Name"]}", Unit = "{r["Unit"]}", Price = {r["Price"]}m, Category = "{r["Category"]}", Supplier = {supplier} }},\n')

    print(f"Extracted {len(final_list)} materials.")

extract_all()
