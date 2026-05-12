import React, { useState } from 'react';
import { Plus, Trash2, Edit3, Package, DollarSign, TrendingUp } from 'lucide-react';
import { useStorage, DATA_KEYS } from '../hooks/useStorage';

const MasterCatalog = () => {
  const [materials, setMaterials] = useStorage(DATA_KEYS.MASTER_MATERIALS, []);
  const [isAdding, setIsAdding] = useState(false);
  const [newItem, setNewItem] = useState({ name: '', unit: '', costPrice: '', basePrice: '' });

  const handleAdd = (e) => {
    e.preventDefault();
    if (!newItem.name || !newItem.unit) return;
    setMaterials([...materials, { ...newItem, id: Date.now() }]);
    setNewItem({ name: '', unit: '', costPrice: '', basePrice: '' });
    setIsAdding(false);
  };

  const handleDelete = (id) => {
    if (confirm('Bạn có chắc chắn muốn xóa vật liệu này?')) {
      setMaterials(materials.filter(m => m.id !== id));
    }
  };

  const formatCurrency = (val) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  return (
    <div className="space-y-6 fade-in">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold">Danh mục vật liệu chung</h1>
          <p className="text-slate-400 text-sm">Quản lý các loại vật liệu và giá nhập/bán tham chiếu.</p>
        </div>
        <button 
          onClick={() => setIsAdding(true)}
          className="btn-primary"
        >
          <Plus className="w-4 h-4" /> Thêm vật liệu
        </button>
      </div>

      {isAdding && (
        <div className="glass-card p-6 border-indigo-500/30">
          <form onSubmit={handleAdd} className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="space-y-2">
              <label className="text-xs text-slate-400 uppercase font-bold tracking-wider">Tên vật liệu</label>
              <input 
                placeholder="VD: Cát xây dựng"
                value={newItem.name}
                onChange={e => setNewItem({...newItem, name: e.target.value})}
                className="w-full"
                required
              />
            </div>
            <div className="space-y-2">
              <label className="text-xs text-slate-400 uppercase font-bold tracking-wider">Đơn vị tính</label>
              <input 
                placeholder="VD: khối, viên, bao"
                value={newItem.unit}
                onChange={e => setNewItem({...newItem, unit: e.target.value})}
                className="w-full"
                required
              />
            </div>
            <div className="space-y-2">
              <label className="text-xs text-slate-400 uppercase font-bold tracking-wider">Giá nhập</label>
              <input 
                type="number"
                placeholder="0"
                value={newItem.costPrice}
                onChange={e => setNewItem({...newItem, costPrice: e.target.value})}
                className="w-full"
              />
            </div>
            <div className="space-y-2">
              <label className="text-xs text-slate-400 uppercase font-bold tracking-wider">Giá bán (mặc định)</label>
              <input 
                type="number"
                placeholder="0"
                value={newItem.basePrice}
                onChange={e => setNewItem({...newItem, basePrice: e.target.value})}
                className="w-full"
              />
            </div>
            <div className="md:col-span-4 flex justify-end gap-3 mt-2">
              <button type="button" onClick={() => setIsAdding(false)} className="btn-secondary text-sm">Hủy</button>
              <button type="submit" className="btn-primary text-sm">Lưu vật liệu</button>
            </div>
          </form>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {materials.length === 0 && (
          <div className="col-span-full py-20 text-center text-slate-500 glass-card">
            <Package className="w-12 h-12 mx-auto mb-4 opacity-20" />
            <p>Chưa có vật liệu nào trong danh mục.</p>
          </div>
        )}
        
        {materials.map(item => (
          <div key={item.id} className="glass-card p-6 group">
            <div className="flex justify-between items-start mb-4">
              <div className="p-3 rounded-xl bg-indigo-500/10 text-indigo-400">
                <Package className="w-6 h-6" />
              </div>
              <button 
                onClick={() => handleDelete(item.id)}
                className="text-slate-500 hover:text-red-400 transition-colors opacity-0 group-hover:opacity-100"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
            
            <h3 className="text-lg font-bold mb-1">{item.name}</h3>
            <p className="text-xs text-slate-500 mb-4 italic">Đơn vị: {item.unit}</p>
            
            <div className="space-y-3 pt-4 border-t border-white/5">
              <div className="flex justify-between items-center text-sm">
                <span className="flex items-center gap-2 text-slate-400">
                  <DollarSign className="w-3 h-3" /> Giá nhập
                </span>
                <span className="font-mono">{formatCurrency(item.costPrice)}</span>
              </div>
              <div className="flex justify-between items-center text-sm">
                <span className="flex items-center gap-2 text-indigo-400">
                  <TrendingUp className="w-3 h-3" /> Giá bán lẻ
                </span>
                <span className="font-bold text-indigo-400">{formatCurrency(item.basePrice)}</span>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default MasterCatalog;
