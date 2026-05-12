import React from 'react';
import { 
  Users, 
  TrendingUp, 
  Package, 
  AlertTriangle,
  ArrowUpRight,
  Wallet
} from 'lucide-react';
import { useStorage, DATA_KEYS } from '../hooks/useStorage';

const Dashboard = () => {
  const [projects] = useStorage(DATA_KEYS.PROJECTS, []);
  const [deliveries] = useStorage(DATA_KEYS.DELIVERIES, []);
  const [materials] = useStorage(DATA_KEYS.MASTER_MATERIALS, []);

  const totalRevenue = deliveries.reduce((sum, d) => sum + d.totalAmount, 0);
  const totalDeliveries = deliveries.length;
  const activeProjects = projects.length;

  const formatCurrency = (val) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  const stats = [
    { label: 'Tổng doanh thu', value: formatCurrency(totalRevenue), icon: Wallet, color: 'text-emerald-400', bg: 'bg-emerald-500/10' },
    { label: 'Dự án đang chạy', value: activeProjects, icon: Users, color: 'text-indigo-400', bg: 'bg-indigo-500/10' },
    { label: 'Số lần giao hàng', value: totalDeliveries, icon: TrendingUp, color: 'text-purple-400', bg: 'bg-purple-500/10' },
    { label: 'Loại vật liệu', value: materials.length, icon: Package, color: 'text-orange-400', bg: 'bg-orange-500/10' },
  ];

  return (
    <div className="space-y-8 fade-in">
      <div>
        <h1 className="text-3xl font-bold font-outfit">Chào buổi chiều, Admin</h1>
        <p className="text-slate-400">Dưới đây là tóm tắt hoạt động kinh doanh kho của bạn.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {stats.map((stat, i) => (
          <div key={i} className="glass-card p-6">
            <div className="flex justify-between items-start mb-4">
              <div className={`p-3 rounded-xl ${stat.bg} ${stat.color}`}>
                <stat.icon className="w-6 h-6" />
              </div>
              <span className="text-emerald-400 text-xs font-bold flex items-center gap-1 bg-emerald-500/5 px-2 py-1 rounded">
                +12% <ArrowUpRight className="w-3 h-3" />
              </span>
            </div>
            <div className="text-2xl font-bold font-outfit mb-1">{stat.value}</div>
            <div className="text-sm text-slate-500">{stat.label}</div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Recent Activity */}
        <div className="glass-card p-6">
          <h3 className="text-xl font-bold mb-6 flex items-center gap-2">
            <TrendingUp className="w-5 h-5 text-indigo-400" /> Hoạt động gần đây
          </h3>
          <div className="space-y-4">
            {deliveries.length === 0 && <p className="text-slate-500 py-4 text-center">Chưa có hoạt động nào.</p>}
            {deliveries.slice(-5).reverse().map(d => {
              const project = projects.find(p => p.id === d.projectId);
              return (
                <div key={d.id} className="flex items-center gap-4 p-3 rounded-xl hover:bg-white/5 transition-colors border border-transparent hover:border-white/5">
                  <div className="w-10 h-10 rounded-full bg-indigo-500/20 flex items-center justify-center text-indigo-400 text-xs font-bold">
                    {project?.customerName?.charAt(0) || '?'}
                  </div>
                  <div className="flex-1">
                    <div className="text-sm font-bold">{project?.customerName}</div>
                    <div className="text-xs text-slate-500">{new Date(d.timestamp).toLocaleString('vi-VN')}</div>
                  </div>
                  <div className="text-sm font-mono font-bold text-indigo-300">
                    +{formatCurrency(d.totalAmount)}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Low Stock Alerts */}
        <div className="glass-card p-6">
          <h3 className="text-xl font-bold mb-6 flex items-center gap-2">
            <AlertTriangle className="w-5 h-5 text-orange-400" /> Cảnh báo sắp hết hàng
          </h3>
          <div className="space-y-4">
            {projects.flatMap(p => p.materials.map(m => ({ ...m, customer: p.customerName })))
              .filter(m => (m.remainingQty / m.totalQty) < 0.2)
              .slice(0, 5)
              .map((m, i) => (
                <div key={i} className="flex items-center justify-between p-3 rounded-xl bg-orange-500/5 border border-orange-500/10">
                  <div>
                    <div className="text-sm font-bold">{m.name}</div>
                    <div className="text-xs text-slate-500">Khách: {m.customer}</div>
                  </div>
                  <div className="text-right">
                    <div className="text-sm font-bold text-orange-400">{m.remainingQty} {m.unit}</div>
                    <div className="text-[10px] text-slate-500 uppercase">Còn dưới 20%</div>
                  </div>
                </div>
              ))
            }
            {projects.every(p => p.materials.every(m => (m.remainingQty / m.totalQty) >= 0.2)) && (
              <p className="text-slate-500 py-4 text-center">Tất cả các dự án đều đủ hàng.</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
