import React, { useState } from 'react';
import { Plus, Users, Search, ChevronRight, MapPin, Phone, Calendar } from 'lucide-react';
import { useStorage, DATA_KEYS } from '../hooks/useStorage';
import ProjectDetail from './ProjectDetail';

const Projects = () => {
  const [projects, setProjects] = useStorage(DATA_KEYS.PROJECTS, []);
  const [materials] = useStorage(DATA_KEYS.MASTER_MATERIALS, []);
  const [selectedProjectId, setSelectedProjectId] = useState(null);
  const [isAdding, setIsAdding] = useState(false);
  
  const [newProject, setNewProject] = useState({
    customerName: '',
    phone: '',
    address: '',
    materials: [] // { masterId, name, unit, customPrice, totalQty, remainingQty }
  });

  const handleAddProject = (e) => {
    e.preventDefault();
    const projectWithId = {
      ...newProject,
      id: Date.now(),
      createdAt: new Date().toISOString(),
      materials: newProject.materials.map(m => ({ ...m, remainingQty: m.totalQty }))
    };
    setProjects([...projects, projectWithId]);
    setIsAdding(false);
    setNewProject({ customerName: '', phone: '', address: '', materials: [] });
  };

  const toggleMaterialSelection = (material) => {
    const exists = newProject.materials.find(m => m.masterId === material.id);
    if (exists) {
      setNewProject({
        ...newProject,
        materials: newProject.materials.filter(m => m.masterId !== material.id)
      });
    } else {
      setNewProject({
        ...newProject,
        materials: [...newProject.materials, {
          masterId: material.id,
          name: material.name,
          unit: material.unit,
          customPrice: material.basePrice,
          totalQty: 0
        }]
      });
    }
  };

  const updateMaterialField = (masterId, field, value) => {
    setNewProject({
      ...newProject,
      materials: newProject.materials.map(m => 
        m.masterId === masterId ? { ...m, [field]: value } : m
      )
    });
  };

  if (selectedProjectId) {
    return <ProjectDetail 
      projectId={selectedProjectId} 
      onBack={() => setSelectedProjectId(null)} 
    />;
  }

  return (
    <div className="space-y-6 fade-in">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold">Quản lý Kho Dự án</h1>
          <p className="text-slate-400 text-sm">Thiết lập kho riêng và giá ưu đãi cho từng khách hàng.</p>
        </div>
        <button 
          onClick={() => setIsAdding(true)}
          className="btn-primary"
        >
          <Plus className="w-4 h-4" /> Thiết lập kho mới
        </button>
      </div>

      {isAdding && (
        <div className="glass-card p-8 border-indigo-500/30 max-w-4xl mx-auto">
          <h2 className="text-xl font-bold mb-6">Thông tin khách hàng & Vật liệu</h2>
          <form onSubmit={handleAddProject} className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-2">
                <label className="text-xs text-slate-400 uppercase font-bold">Tên khách hàng / Dự án</label>
                <input 
                  className="w-full" 
                  placeholder="VD: Anh Tuấn - Nhà phố Quận 7"
                  value={newProject.customerName}
                  onChange={e => setNewProject({...newProject, customerName: e.target.value})}
                  required
                />
              </div>
              <div className="space-y-2">
                <label className="text-xs text-slate-400 uppercase font-bold">Số điện thoại</label>
                <input 
                  className="w-full" 
                  placeholder="09xx xxx xxx"
                  value={newProject.phone}
                  onChange={e => setNewProject({...newProject, phone: e.target.value})}
                />
              </div>
              <div className="md:col-span-2 space-y-2">
                <label className="text-xs text-slate-400 uppercase font-bold">Địa chỉ công trình</label>
                <input 
                  className="w-full" 
                  placeholder="Địa chỉ giao hàng"
                  value={newProject.address}
                  onChange={e => setNewProject({...newProject, address: e.target.value})}
                />
              </div>
            </div>

            <div className="border-t border-white/5 pt-6">
              <h3 className="text-sm font-bold text-slate-300 mb-4 uppercase tracking-wider">Chọn vật liệu & Thiết lập giá</h3>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
                {materials.map(m => {
                  const isSelected = newProject.materials.find(pm => pm.masterId === m.id);
                  return (
                    <button
                      key={m.id}
                      type="button"
                      onClick={() => toggleMaterialSelection(m)}
                      className={`p-3 rounded-xl border text-sm text-left transition-all ${
                        isSelected 
                        ? 'border-indigo-500 bg-indigo-500/10 text-indigo-300' 
                        : 'border-white/5 bg-white/5 text-slate-400 hover:border-white/20'
                      }`}
                    >
                      {m.name} ({m.unit})
                    </button>
                  );
                })}
              </div>

              {newProject.materials.length > 0 && (
                <div className="space-y-4">
                  {newProject.materials.map(m => (
                    <div key={m.masterId} className="flex flex-col md:flex-row gap-4 p-4 rounded-xl bg-white/5 border border-white/5">
                      <div className="flex-1">
                        <div className="text-sm font-bold">{m.name}</div>
                        <div className="text-xs text-slate-500">Đơn vị: {m.unit}</div>
                      </div>
                      <div className="w-full md:w-40 space-y-1">
                        <label className="text-[10px] text-slate-500 uppercase">Giá bán riêng</label>
                        <input 
                          type="number"
                          className="w-full text-sm py-1 px-2"
                          value={m.customPrice}
                          onChange={e => updateMaterialField(m.masterId, 'customPrice', e.target.value)}
                        />
                      </div>
                      <div className="w-full md:w-40 space-y-1">
                        <label className="text-[10px] text-slate-500 uppercase">Tổng số lượng</label>
                        <input 
                          type="number"
                          className="w-full text-sm py-1 px-2"
                          value={m.totalQty}
                          onChange={e => updateMaterialField(m.masterId, 'totalQty', e.target.value)}
                        />
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="flex justify-end gap-3 pt-6 border-t border-white/5">
              <button type="button" onClick={() => setIsAdding(false)} className="btn-secondary">Hủy bỏ</button>
              <button type="submit" className="btn-primary">Tạo kho dự án</button>
            </div>
          </form>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {projects.map(p => (
          <button 
            key={p.id}
            onClick={() => setSelectedProjectId(p.id)}
            className="glass-card p-6 text-left hover:scale-[1.02] transition-transform w-full"
          >
            <div className="flex justify-between items-start mb-4">
              <div className="p-3 rounded-xl bg-purple-500/10 text-purple-400">
                <Users className="w-6 h-6" />
              </div>
              <ChevronRight className="w-5 h-5 text-slate-600" />
            </div>
            <h3 className="text-xl font-bold mb-2">{p.customerName}</h3>
            <div className="space-y-2 text-sm text-slate-400">
              <div className="flex items-center gap-2"><Phone className="w-3 h-3" /> {p.phone || 'N/A'}</div>
              <div className="flex items-center gap-2 line-clamp-1"><MapPin className="w-3 h-3" /> {p.address || 'N/A'}</div>
              <div className="flex items-center gap-2 mt-4 pt-4 border-t border-white/5">
                <Calendar className="w-3 h-3" /> {new Date(p.createdAt).toLocaleDateString('vi-VN')}
              </div>
            </div>
          </button>
        ))}
      </div>
    </div>
  );
};

export default Projects;
