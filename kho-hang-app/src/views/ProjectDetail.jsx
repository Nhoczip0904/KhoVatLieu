import React, { useState } from 'react';
import { 
  ArrowLeft, 
  Truck, 
  History, 
  Receipt, 
  Plus, 
  Minus, 
  Info,
  DollarSign,
  PackageCheck
} from 'lucide-react';
import { useStorage, DATA_KEYS } from '../hooks/useStorage';

const ProjectDetail = ({ projectId, onBack }) => {
  const [projects, setProjects] = useStorage(DATA_KEYS.PROJECTS, []);
  const [deliveries, setDeliveries] = useStorage(DATA_KEYS.DELIVERIES, []);
  
  const project = projects.find(p => p.id === projectId);
  const projectDeliveries = deliveries.filter(d => d.projectId === projectId);
  
  const [isDelivering, setIsDelivering] = useState(false);
  const [deliveryItems, setDeliveryItems] = useState({}); // { masterId: qty }
  const [fees, setFees] = useState({ shipping: 0, other: 0, note: '' });

  if (!project) return <div>Không tìm thấy dự án.</div>;

  // Calculate Previous Balance
  const previousTotal = projectDeliveries.reduce((sum, d) => sum + d.totalAmount, 0);

  const handleDelivery = () => {
    const items = Object.entries(deliveryItems)
      .filter(([_, qty]) => qty > 0)
      .map(([masterId, qty]) => {
        const mat = project.materials.find(m => m.masterId === Number(masterId));
        return {
          masterId: Number(masterId),
          name: mat.name,
          unit: mat.unit,
          price: Number(mat.customPrice),
          qty: Number(qty),
          subtotal: Number(mat.customPrice) * Number(qty)
        };
      });

    if (items.length === 0) return alert('Vui lòng nhập số lượng xuất kho!');

    const itemsTotal = items.reduce((sum, item) => sum + item.subtotal, 0);
    const totalAmount = itemsTotal + Number(fees.shipping) + Number(fees.other);

    const newDelivery = {
      id: `INV-${Date.now()}`,
      projectId,
      timestamp: new Date().toISOString(),
      items,
      fees,
      itemsTotal,
      previousBalance: previousTotal,
      totalAmount,
      grandTotal: totalAmount + previousTotal
    };

    // Update Remaining Qty in Project
    const updatedProjects = projects.map(p => {
      if (p.id === projectId) {
        return {
          ...p,
          materials: p.materials.map(m => {
            const delivered = deliveryItems[m.masterId] || 0;
            return { ...m, remainingQty: Number(m.remainingQty) - Number(delivered) };
          })
        };
      }
      return p;
    });

    setDeliveries([...deliveries, newDelivery]);
    setProjects(updatedProjects);
    setIsDelivering(false);
    setDeliveryItems({});
    setFees({ shipping: 0, other: 0, note: '' });
  };

  const formatCurrency = (val) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  return (
    <div className="space-y-8 fade-in">
      <button onClick={onBack} className="flex items-center gap-2 text-slate-400 hover:text-white transition-colors">
        <ArrowLeft className="w-4 h-4" /> Quay lại danh sách
      </button>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left: Project Info & Stock */}
        <div className="lg:col-span-2 space-y-6">
          <div className="glass-card p-6">
            <h1 className="text-3xl font-bold mb-2">{project.customerName}</h1>
            <div className="flex flex-wrap gap-4 text-sm text-slate-400">
              <span className="flex items-center gap-1"><Truck className="w-4 h-4" /> {project.address}</span>
              <span className="flex items-center gap-1"><Receipt className="w-4 h-4" /> Nợ lũy kế: <b className="text-orange-400">{formatCurrency(previousTotal)}</b></span>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {project.materials.map(m => {
              const progress = (m.remainingQty / m.totalQty) * 100;
              return (
                <div key={m.masterId} className="glass-card p-5 relative overflow-hidden">
                  <div className="absolute bottom-0 left-0 h-1 bg-indigo-500/20 w-full" />
                  <div 
                    className="absolute bottom-0 left-0 h-1 bg-indigo-500 transition-all duration-1000" 
                    style={{ width: `${progress}%` }} 
                  />
                  
                  <div className="flex justify-between items-start mb-4">
                    <div>
                      <h4 className="font-bold text-lg">{m.name}</h4>
                      <p className="text-xs text-slate-500 italic">Đơn vị: {m.unit} | Giá: {formatCurrency(m.customPrice)}</p>
                    </div>
                    <div className="text-right">
                      <div className="text-2xl font-bold text-indigo-400">{m.remainingQty}</div>
                      <div className="text-[10px] text-slate-500 uppercase tracking-tighter">Còn lại / {m.totalQty}</div>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>

          {/* Delivery History */}
          <div className="glass-card p-6">
            <div className="flex items-center gap-2 mb-6">
              <History className="w-5 h-5 text-indigo-400" />
              <h3 className="text-xl font-bold">Lịch sử xuất kho</h3>
            </div>
            
            <div className="space-y-4">
              {projectDeliveries.length === 0 && <p className="text-slate-500 text-center py-8">Chưa có lần giao hàng nào.</p>}
              {projectDeliveries.slice().reverse().map(d => (
                <div key={d.id} className="p-4 rounded-xl border border-white/5 bg-white/5 flex flex-col md:flex-row justify-between items-center gap-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                      <span className="text-xs font-mono text-indigo-400 bg-indigo-500/10 px-2 py-0.5 rounded">{d.id}</span>
                      <span className="text-sm text-slate-400">{new Date(d.timestamp).toLocaleString('vi-VN')}</span>
                    </div>
                    <div className="text-xs text-slate-500">
                      Xuất: {d.items.map(i => `${i.qty} ${i.unit} ${i.name}`).join(', ')}
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="text-lg font-bold">{formatCurrency(d.totalAmount)}</div>
                    <div className="text-[10px] text-slate-500">Tổng lũy kế: {formatCurrency(d.grandTotal)}</div>
                  </div>
                  <button className="p-2 hover:bg-white/10 rounded-lg text-slate-400 transition-colors">
                    <Receipt className="w-5 h-5" />
                  </button>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Right: Quick Delivery Action */}
        <div className="space-y-6">
          <div className="glass-card p-6 border-indigo-500/20 bg-indigo-500/5">
            <h3 className="text-xl font-bold mb-4 flex items-center gap-2">
              <PackageCheck className="w-5 h-5 text-indigo-400" /> Xuất hàng mới
            </h3>
            
            <div className="space-y-4">
              {project.materials.map(m => (
                <div key={m.masterId} className="space-y-2">
                  <div className="flex justify-between text-xs text-slate-400">
                    <span>{m.name} ({m.unit})</span>
                    <span>Tồn: {m.remainingQty}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <input 
                      type="number"
                      placeholder="Số lượng"
                      className="flex-1 text-sm py-2"
                      value={deliveryItems[m.masterId] || ''}
                      onChange={e => setDeliveryItems({...deliveryItems, [m.masterId]: e.target.value})}
                    />
                  </div>
                </div>
              ))}
              
              <div className="border-t border-white/10 pt-4 space-y-3">
                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1">
                    <label className="text-[10px] text-slate-500 uppercase">Phí vận chuyển</label>
                    <input 
                      type="number"
                      className="w-full text-sm"
                      value={fees.shipping}
                      onChange={e => setFees({...fees, shipping: e.target.value})}
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="text-[10px] text-slate-500 uppercase">Phí khác</label>
                    <input 
                      type="number"
                      className="w-full text-sm"
                      value={fees.other}
                      onChange={e => setFees({...fees, other: e.target.value})}
                    />
                  </div>
                </div>
              </div>

              <button 
                onClick={handleDelivery}
                className="btn-primary w-full justify-center mt-4 py-3"
              >
                Xác nhận xuất kho & In Hóa đơn
              </button>
            </div>
          </div>

          <div className="glass-card p-6 border-orange-500/20 bg-orange-500/5">
            <h3 className="text-lg font-bold mb-2 flex items-center gap-2">
              <Info className="w-5 h-5 text-orange-400" /> Lưu ý
            </h3>
            <p className="text-xs text-slate-400 leading-relaxed">
              Mỗi lần bấm "Xác nhận", hệ thống sẽ tự động trừ đi số lượng vật liệu trong kho của dự án này và tạo một hóa đơn ghi nhận nợ mới.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProjectDetail;
